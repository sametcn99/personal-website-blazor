#!/usr/bin/env bun
/**
 * content/ klasörünü baz alarak her içerik için ayrı bir kapak görseli üretir.
 *
 * Akış (her içerik için):
 *   1) content/ altındaki .mdx dosyasını oku, context çıkar (başlık, etiketler,
 *      tip ve gövde metni — frontmatter'daki summary hiç kullanılmaz).
 *   2) Context'i bir metin modeline (OpenRouter) gönderip, sabit stil talimatına
 *      uygun, o içeriğe özel bir görsel üretme prompt'u yazdır.
 *   3) O prompt'u görsel üretme modeline (OpenRouter) gönder.
 *   4) Dönen görseli 960x540'a normalize edip, content/ ile aynı klasör
 *      hiyerarşisini koruyarak wwwroot/content/assets altına kaydet.
 *
 * content/ altındaki klasör yapısı sabit kodlanmamıştır; script content/
 * altını recursive olarak tarar ve aynı hiyerarşiyi wwwroot/content/assets
 * altında yeniden oluşturur.
 *
 * Kullanım:
 *   bun run generate:images                          # tüm içerikler, var olanları atla
 *   bun run generate:images -- --force                 # hepsini yeniden üret
 *   bun run generate:images -- --content posts/arcdrop # tek bir içerik (göreli yol)
 *   bun run generate:images -- --content arcdrop       # tek bir içerik (sadece slug)
 *   bun run generate:images -- --dry-run                # context'i yazdır, API çağırma, dosya yazma
 */

import { parseArgs } from "util";
import { readdir, readFile, mkdir, rename } from "fs/promises";
import { existsSync } from "fs";
import path from "path";
import matter from "gray-matter";
import sharp from "sharp";

const ROOT = path.resolve(import.meta.dir, "..");
const CONTENT_DIR = path.join(ROOT, "content");
const ASSETS_DIR = path.join(ROOT, "wwwroot", "content", "assets");

const IMAGE_WIDTH = 960;
const IMAGE_HEIGHT = 540;

// Placeholder'lar — çalıştırmadan önce gerçek OpenRouter modelleriyle değiştirin.
const OPENROUTER_TEXT_MODEL =
  process.env.OPENROUTER_TEXT_MODEL ?? "REPLACE_ME/choose-a-text-model";
const OPENROUTER_IMAGE_MODEL =
  process.env.OPENROUTER_IMAGE_MODEL ?? "REPLACE_ME/choose-an-image-model";

const OPENROUTER_API_KEY = process.env.OPENROUTER_API_KEY;
const OPENROUTER_URL = "https://openrouter.ai/api/v1/chat/completions";

// Sabit stil talimatı: her içerik için aynı kalır, prompt yazan modele de,
// (fallback olarak) doğrudan görsel modele de bu stil iletilir.
const STYLE_INSTRUCTION =
  "A dark, abstract, minimal cover illustration for a technical article. " +
  "Moody near-black background, muted geometric shapes, soft olive-green and " +
  "warm amber accent colors, subtle grid lines, restrained and professional, " +
  "no text, no watermark, no UI chrome, 16:9 composition.";

// Prompt yazan metin modeline verilen sabit talimat.
const PROMPT_WRITER_INSTRUCTION =
  "You write concise, vivid prompts for an image-generation model that creates " +
  "cover illustrations for technical articles. You will be given a content's " +
  "title, type, tags, and body text. Read and summarize the content yourself, " +
  "then write a single-paragraph English image-generation prompt for its cover " +
  `illustration. The prompt must follow this exact visual style: "${STYLE_INSTRUCTION}" ` +
  "Ground the imagery in what the content is actually about. Output only the " +
  "final image-generation prompt text, nothing else — no preamble, no quotes, no labels.";

const MAX_RETRIES = 3;
const RETRY_BASE_DELAY_MS = 1500;
const BODY_MAX_CHARS = 6000;

interface ContentFile {
  /** content/ köküne göre göreli yol, uzantısız (örn. "posts/arcdrop") */
  relativePath: string;
  absolutePath: string;
}

interface ContentContext {
  relativePath: string;
  title: string;
  tags: string[];
  type: string;
  /** Frontmatter'ı çıkarılmış, hafifçe temizlenmiş ham gövde metni. */
  body: string;
}

interface RunSummary {
  generated: string[];
  skipped: string[];
  failed: { item: string; error: string }[];
}

function parseCliArgs() {
  const { values } = parseArgs({
    args: Bun.argv.slice(2),
    options: {
      content: { type: "string" },
      force: { type: "boolean", default: false },
      "dry-run": { type: "boolean", default: false },
    },
    strict: true,
  });

  return {
    contentFilter: values.content?.trim() || null,
    force: values.force ?? false,
    dryRun: values["dry-run"] ?? false,
  };
}

/** content/ altını recursive olarak tarayıp tüm .mdx dosyalarını bulur. */
async function discoverContentFiles(dir: string): Promise<ContentFile[]> {
  const entries = await readdir(dir, { withFileTypes: true });
  const files: ContentFile[] = [];

  for (const entry of entries) {
    const absolutePath = path.join(dir, entry.name);

    if (entry.isDirectory()) {
      files.push(...(await discoverContentFiles(absolutePath)));
      continue;
    }

    if (entry.isFile() && entry.name.endsWith(".mdx")) {
      const relativePath = path
        .relative(CONTENT_DIR, absolutePath)
        .replace(/\.mdx$/, "")
        .split(path.sep)
        .join("/");
      files.push({ relativePath, absolutePath });
    }
  }

  return files;
}

/** Gövdeyi modele göndermeden önce gürültüyü (kod bloğu, görsel embed) hafifçe temizler. Özetleme yapmaz. */
function cleanBody(body: string): string {
  return body
    .replace(/```[\s\S]*?```/g, " ")
    .replace(/!\[[^\]]*\]\([^)]*\)/g, " ")
    .replace(/\s+/g, " ")
    .trim()
    .slice(0, BODY_MAX_CHARS);
}

async function extractContext(file: ContentFile): Promise<ContentContext> {
  const raw = await readFile(file.absolutePath, "utf8");
  const { data, content } = matter(raw);

  const section = file.relativePath.split("/")[0] ?? "content";
  const slug = file.relativePath.split("/").pop() ?? file.relativePath;

  return {
    relativePath: file.relativePath,
    title: data.title ?? slug,
    tags: Array.isArray(data.tags) ? data.tags : [],
    type: data.type ?? section,
    body: cleanBody(content),
  };
}

function outputPath(relativePath: string): string {
  return path.join(ASSETS_DIR, ...relativePath.split("/")) + ".png";
}

function sleep(ms: number) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

async function callOpenRouter(payload: Record<string, unknown>): Promise<any> {
  if (!OPENROUTER_API_KEY) {
    throw new Error("OPENROUTER_API_KEY tanımlı değil (.env içinde bekleniyor)");
  }

  let lastError: unknown;

  for (let attempt = 1; attempt <= MAX_RETRIES; attempt++) {
    try {
      const response = await fetch(OPENROUTER_URL, {
        method: "POST",
        headers: {
          Authorization: `Bearer ${OPENROUTER_API_KEY}`,
          "Content-Type": "application/json",
        },
        body: JSON.stringify(payload),
      });

      if (!response.ok) {
        const body = await response.text();
        throw new Error(`OpenRouter ${response.status}: ${body.slice(0, 300)}`);
      }

      return await response.json();
    } catch (err) {
      lastError = err;
      if (attempt < MAX_RETRIES) {
        await sleep(RETRY_BASE_DELAY_MS * attempt);
      }
    }
  }

  throw lastError instanceof Error ? lastError : new Error(String(lastError));
}

/** Metin modeli: içeriği okuyup özetler ve o içeriğe özel bir görsel üretme prompt'u yazar. */
async function writeImagePrompt(context: ContentContext): Promise<string> {
  const tagLine = context.tags.length > 0 ? `Tags: ${context.tags.join(", ")}` : "";
  const userMessage = [
    `Title: ${context.title}`,
    `Type: ${context.type}`,
    tagLine,
    "Body:",
    context.body,
  ]
    .filter(Boolean)
    .join("\n");

  const json = await callOpenRouter({
    model: OPENROUTER_TEXT_MODEL,
    messages: [
      { role: "system", content: PROMPT_WRITER_INSTRUCTION },
      { role: "user", content: userMessage },
    ],
  });

  const text: string | undefined = json?.choices?.[0]?.message?.content?.trim();
  if (!text) {
    throw new Error("Metin modeli boş bir prompt döndürdü");
  }

  return text;
}

/** Görsel üretme aracı: yazılmış prompt'u OpenRouter'a gönderir, ham görsel verisini döner. */
async function generateImage(prompt: string): Promise<Buffer> {
  const json = await callOpenRouter({
    model: OPENROUTER_IMAGE_MODEL,
    modalities: ["image", "text"],
    messages: [{ role: "user", content: prompt }],
  });

  const images = json?.choices?.[0]?.message?.images;
  const dataUrl: string | undefined = images?.[0]?.image_url?.url;

  if (!dataUrl) {
    throw new Error("OpenRouter yanıtında görsel bulunamadı");
  }

  const base64 = dataUrl.includes(",") ? dataUrl.split(",", 2)[1] : dataUrl;
  return Buffer.from(base64, "base64");
}

/** Ham görseli 960x540'a normalize edip hedef yola atomik olarak yazar. */
async function saveImage(raw: Buffer, targetPath: string): Promise<void> {
  const resized = await sharp(raw)
    .resize(IMAGE_WIDTH, IMAGE_HEIGHT, { fit: "cover", position: "attention" })
    .png()
    .toBuffer();

  await mkdir(path.dirname(targetPath), { recursive: true });
  const tmpPath = `${targetPath}.tmp-${process.pid}`;
  await Bun.write(tmpPath, resized);
  await rename(tmpPath, targetPath);
}

async function processContent(
  file: ContentFile,
  opts: { force: boolean; dryRun: boolean },
  summary: RunSummary,
): Promise<void> {
  const label = file.relativePath;
  const targetPath = outputPath(label);

  if (!opts.force && existsSync(targetPath)) {
    console.log(`skip  ${label} (zaten var)`);
    summary.skipped.push(label);
    return;
  }

  const context = await extractContext(file);

  if (opts.dryRun) {
    const bodyPreview = context.body.slice(0, 200);
    console.log(
      `dry-run ${label}\n` +
        `  title: ${context.title}\n` +
        `  type: ${context.type}\n` +
        `  tags: ${context.tags.join(", ") || "(yok)"}\n` +
        `  body preview: ${bodyPreview}${context.body.length > 200 ? "..." : ""}\n` +
        `  -> ${targetPath}\n` +
        `  (prompt metin modeliyle üretilir, dry-run'da API çağrılmaz)`,
    );
    return;
  }

  console.log(`gen   ${label}`);

  try {
    const prompt = await writeImagePrompt(context);
    console.log(`prompt ${label}: ${prompt}`);

    const raw = await generateImage(prompt);
    await saveImage(raw, targetPath);

    console.log(`done  ${label} -> ${path.relative(ROOT, targetPath)}`);
    summary.generated.push(label);
  } catch (err) {
    const message = err instanceof Error ? err.message : String(err);
    console.error(`fail  ${label}: ${message}`);
    summary.failed.push({ item: label, error: message });
  }
}

function resolveContentFilter(files: ContentFile[], filter: string): ContentFile[] {
  const exact = files.filter((f) => f.relativePath === filter);
  if (exact.length > 0) return exact;

  const bySlug = files.filter((f) => f.relativePath.split("/").pop() === filter);
  return bySlug;
}

async function main() {
  const { contentFilter, force, dryRun } = parseCliArgs();

  if (!dryRun && !OPENROUTER_API_KEY) {
    console.error("OPENROUTER_API_KEY tanımlı değil. Çalıştırmadan önce .env içine ekleyin.");
    process.exitCode = 1;
    return;
  }

  const allFiles = await discoverContentFiles(CONTENT_DIR);
  const files = contentFilter ? resolveContentFilter(allFiles, contentFilter) : allFiles;

  if (files.length === 0) {
    console.log(
      contentFilter
        ? `"${contentFilter}" ile eşleşen içerik bulunamadı.`
        : "content/ altında .mdx dosyası bulunamadı.",
    );
    return;
  }

  const summary: RunSummary = { generated: [], skipped: [], failed: [] };

  for (const file of files) {
    await processContent(file, { force, dryRun }, summary);
  }

  if (dryRun) return;

  console.log("\n--- özet ---");
  console.log(`üretildi: ${summary.generated.length}`);
  console.log(`atlandı:  ${summary.skipped.length}`);
  console.log(`hata:     ${summary.failed.length}`);
  if (summary.failed.length > 0) {
    for (const f of summary.failed) console.log(`  - ${f.item}: ${f.error}`);
    process.exitCode = 1;
  }
}

main();
