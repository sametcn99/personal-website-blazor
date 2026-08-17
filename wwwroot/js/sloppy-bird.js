const palette = {
  skyTop: "#171914",
  skyBottom: "#292d22",
  haze: "#4c523d",
  cityFar: "#1c1e19",
  cityNear: "#24271f",
  windows: "#75623a",
  hillFar: "#394331",
  hillNear: "#4f6043",
  foliage: "#687b59",
  foliageLight: "#7f9870",
  ground: "#262a21",
  groundTop: "#c89a49",
  pipe: "#667957",
  pipeLight: "#849775",
  pipeDark: "#35402f",
  pipeEdge: "#c89a49",
  birdBody: "#a87a3f",
  birdBack: "#735538",
  birdChest: "#d7c28f",
  birdLight: "#ead9aa",
  birdWing: "#654b31",
  birdFeather: "#3f3123",
  birdDark: "#30251a",
  birdCrown: "#57402b",
  birdLeg: "#9c7548",
  beak: "#bd9046",
  eye: "#c9b983",
  pupil: "#0e0f0d",
}

const TAU = Math.PI * 2
const clamp = (value, minimum, maximum) => Math.max(minimum, Math.min(maximum, value))
const smoothstep = (value) => {
  const t = clamp(value, 0, 1)
  return t * t * (3 - 2 * t)
}

const randomFrom = (seed) => {
  const value = Math.sin(seed * 12.9898) * 43758.5453
  return value - Math.floor(value)
}

// Birds do not move their wings with a sine wave: the power stroke is fast,
// while the recovery stroke is slower and more controlled.
const wingPoseFromPhase = (phase) => {
  const cycle = (((phase / TAU) % 1) + 1) % 1
  if (cycle < 0.36) return 1 - smoothstep(cycle / 0.36)
  return smoothstep((cycle - 0.36) / 0.64)
}

export function mount(host) {
  host.innerHTML = `
    <canvas aria-label="Sloppy Bird game. Click, tap, or press Space to fly." role="application" tabindex="0"></canvas>
    <div class="sloppy-bird-hud" aria-live="off" hidden><span>Score</span><strong>0</strong></div>
    <div class="sloppy-bird-overlay">
      <strong><span class="sloppy-bird-title-word">Sloppy</span><span class="sloppy-bird-title-accent">Bird</span></strong>
      <span>Click, tap, or press Space to fly</span>
      <small>Clear the pipes. Stay off the ground.</small>
      <button type="button" data-umami-event="sloppy-bird-play-click">Play</button>
    </div>`

  const canvas = host.querySelector("canvas")
  Object.assign(host.style, {
    position: "relative",
    width: "100%",
    height: "clamp(19rem, 34vw, 24rem)",
    overflow: "hidden",
  })
  Object.assign(canvas.style, {
    display: "block",
    width: "100%",
    height: "100%",
  })
  const context = canvas.getContext("2d", { alpha: false, desynchronized: true })
  const scoreHud = host.querySelector(".sloppy-bird-hud")
  const scoreValue = scoreHud.querySelector("strong")
  const overlay = host.querySelector(".sloppy-bird-overlay")
  const prefersReducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)")
  const groundHeight = 34
  let width = 0
  let height = 0
  let pixelRatio = 0
  let frame = 0
  let previousTime = 0
  let score = 0
  let state = "ready"
  let sceneOffset = 0
  let ambientTime = 0
  let isVisible = true
  let bird
  let pipes = []
  let buildings = []
  let shrubs = []
  let fireworks = []
  let freedomFlight = null
  let gameOverPipe = null
  let gameOverBirdOffsetX = 0
  let spawnAfter = 0
  let hasSpawnedOpeningPipe = false
  let skyGradient

  const buildScenery = () => {
    buildings = []
    shrubs = []
    const sceneryWidth = width + 240

    for (let x = 0, index = 0; x < sceneryWidth; index += 1) {
      const buildingWidth = 38 + Math.round(randomFrom(index + 4) * 54)
      buildings.push({
        x,
        width: buildingWidth,
        height: 48 + Math.round(randomFrom(index + 19) * Math.min(92, height * 0.28)),
        lit: randomFrom(index + 37) > 0.52,
      })
      x += buildingWidth + 8 + Math.round(randomFrom(index + 51) * 14)
    }

    for (let x = 0, index = 0; x < sceneryWidth; index += 1) {
      const radius = 18 + Math.round(randomFrom(index + 72) * 24)
      shrubs.push({ x, radius, lift: randomFrom(index + 91) * 9 })
      x += radius * 1.35
    }
  }

  const reset = () => {
    bird = {
      x: Math.max(88, width * 0.22),
      y: Math.max(70, (height - groundHeight) * 0.47),
      velocity: 0,
      radius: 12,
      wingPhase: Math.random() * TAU,
      flapEnergy: 0,
    }
    pipes.length = 0
    fireworks.length = 0
    gameOverPipe = null
    gameOverBirdOffsetX = 0
    score = 0
    ambientTime = 0
    spawnAfter = 0.3
    hasSpawnedOpeningPipe = false
    scoreValue.textContent = "0"
  }

  const resize = () => {
    const rect = host.getBoundingClientRect()
    const pixelArea = rect.width * rect.height
    const nextRatio = Math.min(window.devicePixelRatio || 1, pixelArea > 700000 ? 1 : 1.5)
    const nextWidth = Math.max(1, Math.round(rect.width))
    const nextHeight = Math.max(1, Math.round(rect.height))
    if (width === nextWidth && height === nextHeight && pixelRatio === nextRatio) return

    width = nextWidth
    height = nextHeight
    pixelRatio = nextRatio
    canvas.width = Math.round(width * pixelRatio)
    canvas.height = Math.round(height * pixelRatio)
    context.setTransform(pixelRatio, 0, 0, pixelRatio, 0, 0)
    skyGradient = context.createLinearGradient(0, 0, 0, height)
    skyGradient.addColorStop(0, palette.skyTop)
    skyGradient.addColorStop(0.66, palette.skyBottom)
    skyGradient.addColorStop(1, palette.haze)
    buildScenery()
    if (state === "ready" || state === "over") reset()
    draw()
  }

  const spawnPipe = () => {
    const playableHeight = height - groundHeight
    const gap = Math.max(108, Math.min(146, playableHeight * 0.43))
    const margin = 30
    const available = Math.max(1, playableHeight - gap - margin * 2)
    pipes.push({
      x: width + 54,
      opening: margin + gap / 2 + Math.random() * available,
      gap,
      scored: false,
      isOpeningPipe: !hasSpawnedOpeningPipe,
    })
    hasSpawnedOpeningPipe = true
  }

  const showGameOver = (collisionPipe = null) => {
    if (state !== "playing") return
    gameOverPipe = collisionPipe
    gameOverBirdOffsetX = collisionPipe && bird ? bird.x - collisionPipe.x : 0
    state = "over"
    fireworks.length = 0
    scoreHud.hidden = true
    overlay.innerHTML = `<strong><span class="sloppy-bird-title-word">Sloppy</span><span class="sloppy-bird-title-accent">Bird</span></strong><span>Game over · Score: ${score}</span><small>You cleared ${score} ${score === 1 ? "pipe" : "pipes"}.</small><button type="button" data-umami-event="sloppy-bird-play-again-click">Play again</button>`
    overlay.hidden = false
    overlay.querySelector("button").focus({ preventScroll: true })
  }

  const freeBird = () => {
    if (state !== "playing" || score !== 0 || !bird) return

    const canvasRect = canvas.getBoundingClientRect()
    const startPoint = { x: canvasRect.left + bird.x, y: canvasRect.top + Math.max(8, bird.y) }
    state = "freed"
    fireworks.length = 0
    scoreHud.hidden = true
    overlay.hidden = true
    bird = null

    freedomFlight = startFreedomFlight(startPoint)
  }

  const start = () => {
    reset()
    state = "playing"
    scoreHud.hidden = false
    overlay.style.cursor = ""
    overlay.hidden = true
    canvas.focus({ preventScroll: true })
    resume()
  }

  const flap = () => {
    if (state === "ready") start()
    if (state !== "playing") return
    bird.velocity = -292
    bird.flapEnergy = 1
  }

  const celebrateMilestone = () => {
    const reducedMotion = prefersReducedMotion.matches
    const colors = [palette.pipeEdge, palette.birdLight, palette.foliageLight, palette.eye]
    const burstCount = width <= 640 || reducedMotion ? 2 : 4
    const burstZones = [0.2, 0.4, 0.6, 0.8]
    for (let index = burstZones.length - 1; index > 0; index -= 1) {
      const swapIndex = Math.floor(Math.random() * (index + 1))
      const zone = burstZones[index]
      burstZones[index] = burstZones[swapIndex]
      burstZones[swapIndex] = zone
    }

    for (let burst = 0; burst < burstCount; burst += 1) {
      const centerX = width * (burstZones[burst] + (Math.random() - 0.5) * 0.1)
      const centerY = height - groundHeight - (94 + Math.random() * Math.min(108, height * 0.27))
      const particleCount = reducedMotion ? 10 : width <= 640 ? 16 : 22
      const primaryColor = colors[burst % (colors.length - 1)]
      const delay = burst === 0 ? 0 : burst * (reducedMotion ? 0.08 : 0.12) + Math.random() * (reducedMotion ? 0.04 : 0.14)

      for (let index = 0; index < particleCount; index += 1) {
        const angle = (Math.PI * 2 * index) / particleCount + (Math.random() - 0.5) * 0.2
        const speed = reducedMotion ? 0 : 54 + Math.random() * 52
        const startRadius = reducedMotion ? 10 : 3
        fireworks.push({
          x: centerX + Math.cos(angle) * startRadius,
          y: centerY + Math.sin(angle) * startRadius,
          velocityX: Math.cos(angle) * speed,
          velocityY: Math.sin(angle) * speed,
          age: 0,
          delay,
          life: reducedMotion ? 0.72 : 1.02 + Math.random() * 0.42,
          color: index % 6 === 0 ? colors[3] : primaryColor,
          size: 1.15 + Math.random() * 0.75,
          twinkle: Math.random() * Math.PI * 2,
          reducedMotion,
        })
      }
    }
  }

  const update = (delta) => {
    const isAmbient = state === "ready" || state === "over"

    // Keep the scenery moving while the game is idle or after a round ends.
    // Gameplay physics still run only while actively playing.
    if (state === "playing" || state === "freed" || isAmbient) {
      sceneOffset = (sceneOffset + 30 * delta) % Math.max(1, width + 240)
    }
    if (isAmbient) {
      ambientTime += delta
      if (bird) {
        if (state === "ready") {
          bird.wingPhase = (bird.wingPhase + delta * 11) % TAU
          bird.y = Math.max(70, (height - groundHeight) * 0.47) + Math.sin(ambientTime * 2.4) * 3
        }
      }
      if (state === "over") {
        const pipeSpeed = 145
        for (let index = pipes.length - 1; index >= 0; index -= 1) {
          const pipe = pipes[index]
          pipe.x -= pipeSpeed * delta
          if (pipe.x + 48 < -8) pipes.splice(index, 1)
        }
        if (bird) {
          if (gameOverPipe) bird.x = gameOverPipe.x + gameOverBirdOffsetX
          else bird.x -= pipeSpeed * delta
        }
      }
      return
    }
    if (state === "freed") {
      const pipeSpeed = 145
      for (let index = pipes.length - 1; index >= 0; index -= 1) {
        const pipe = pipes[index]
        pipe.x -= pipeSpeed * delta
        if (pipe.x + 48 < -8) pipes.splice(index, 1)
      }
      return
    }
    if (state !== "playing") return

    bird.velocity += 810 * delta
    bird.y += bird.velocity * delta
    bird.flapEnergy = Math.max(0, bird.flapEnergy - delta * 3.4)
    const wingCadence = 18 + bird.flapEnergy * 9 + Math.max(0, -bird.velocity) * 0.012
    bird.wingPhase = (bird.wingPhase + delta * wingCadence) % TAU

    for (let index = fireworks.length - 1; index >= 0; index -= 1) {
      const particle = fireworks[index]
      particle.age += delta
      if (particle.age >= particle.delay + particle.life) {
        fireworks.splice(index, 1)
        continue
      }
      if (particle.age < particle.delay) continue

      particle.x += particle.velocityX * delta
      particle.y += particle.velocityY * delta
      if (!particle.reducedMotion) {
        const drag = Math.pow(0.982, delta * 60)
        particle.velocityX *= drag
        particle.velocityY = particle.velocityY * drag + 26 * delta
      }
    }

    let pipeSpeed = 145
    const openingPipe = pipes[0]?.isOpeningPipe ? pipes[0] : null
    if (!hasSpawnedOpeningPipe) {
      pipeSpeed = 360
    } else if (openingPipe) {
      const fastZoneEnd = width * 0.68
      const normalZoneStart = Math.min(fastZoneEnd - 40, Math.max(width * 0.5, bird.x + Math.min(280, width * 0.22)))

      if (openingPipe.x > fastZoneEnd) {
        pipeSpeed = 360
      } else if (openingPipe.x > normalZoneStart) {
        const transition = (openingPipe.x - normalZoneStart) / Math.max(1, fastZoneEnd - normalZoneStart)
        pipeSpeed = 145 + 215 * transition
      }
    }

    spawnAfter -= delta * (pipeSpeed / 145)
    if (spawnAfter <= 0) {
      spawnPipe()
      spawnAfter = 1.62
    }

    for (let index = pipes.length - 1; index >= 0; index -= 1) {
      const pipe = pipes[index]
      pipe.x -= pipeSpeed * delta
      if (!pipe.scored && pipe.x + 48 < bird.x) {
        pipe.scored = true
        score += 1
        scoreValue.textContent = String(score)
        if (score % 10 === 0) celebrateMilestone()
      }
      const overlaps = bird.x + bird.radius > pipe.x && bird.x - bird.radius < pipe.x + 48
      const insideGap = bird.y - bird.radius > pipe.opening - pipe.gap / 2 && bird.y + bird.radius < pipe.opening + pipe.gap / 2
      if (overlaps && !insideGap) showGameOver(pipe)
      if (pipe.x + 48 < -8) pipes.splice(index, 1)
    }

    if (bird.y - bird.radius < 0) {
      if (score === 0) freeBird()
      else showGameOver()
    } else if (bird.y + bird.radius > height - groundHeight) {
      showGameOver()
    }
  }

  const drawBackground = () => {
    context.fillStyle = skyGradient || palette.skyTop
    context.fillRect(0, 0, width, height)
    context.globalAlpha = 0.16
    context.fillStyle = palette.pipeEdge
    context.beginPath()
    context.arc(width * 0.78, height * 0.22, Math.min(42, height * 0.12), 0, Math.PI * 2)
    context.fill()
    context.globalAlpha = 1

    context.fillStyle = palette.hillFar
    context.beginPath()
    context.moveTo(0, height - 62)
    for (let x = -80; x <= width + 120; x += 120) {
      const shifted = x - ((sceneOffset * 0.16) % 120)
      context.quadraticCurveTo(shifted + 60, height - 142, shifted + 120, height - 62)
    }
    context.lineTo(width, height)
    context.lineTo(0, height)
    context.fill()

    if (state === "playing" && width > 640) {
      const scoreSize = Math.max(88, Math.min(156, height * 0.38))
      context.save()
      context.globalAlpha = 0.13
      context.fillStyle = palette.eye
      context.font = `700 ${scoreSize}px "Familjen Grotesk", sans-serif`
      context.textAlign = "center"
      context.textBaseline = "middle"
      context.fillText(String(score), width * 0.5, height * 0.48)
      context.restore()
    }

    const cityLoop = width + 240
    for (let repeat = -1; repeat <= 1; repeat += 1) {
      for (let index = 0; index < buildings.length; index += 1) {
        const item = buildings[index]
        const x = item.x - ((sceneOffset * 0.34) % cityLoop) + repeat * cityLoop
        const y = height - groundHeight - item.height - 18
        if (x + item.width < -2 || x > width + 2) continue
        context.fillStyle = index % 2 ? palette.cityFar : palette.cityNear
        context.fillRect(x, y, item.width, item.height + 18)
        if (item.lit) {
          context.fillStyle = palette.windows
          context.globalAlpha = 0.5
          for (let windowY = y + 13; windowY < height - groundHeight - 10; windowY += 18) {
            context.fillRect(x + 10, windowY, 4, 5)
            if (item.width > 58) context.fillRect(x + 28, windowY, 4, 5)
          }
          context.globalAlpha = 1
        }
      }
    }

    context.fillStyle = palette.hillNear
    context.beginPath()
    context.moveTo(0, height - groundHeight)
    for (let x = -60; x <= width + 100; x += 90) {
      const shifted = x - ((sceneOffset * 0.58) % 90)
      context.quadraticCurveTo(shifted + 45, height - 98, shifted + 90, height - groundHeight)
    }
    context.lineTo(width, height)
    context.lineTo(0, height)
    context.fill()

    const shrubLoop = width + 240
    for (let repeat = -1; repeat <= 1; repeat += 1) {
      for (const shrub of shrubs) {
        const x = shrub.x - ((sceneOffset * 0.82) % shrubLoop) + repeat * shrubLoop
        const y = height - groundHeight + shrub.lift
        if (x + shrub.radius < -2 || x - shrub.radius > width + 2) continue
        context.fillStyle = palette.foliage
        context.beginPath()
        context.arc(x, y, shrub.radius, Math.PI, Math.PI * 2)
        context.fill()
        context.fillStyle = palette.foliageLight
        context.globalAlpha = 0.22
        context.beginPath()
        context.arc(x - shrub.radius * 0.24, y - 2, shrub.radius * 0.58, Math.PI, Math.PI * 2)
        context.fill()
        context.globalAlpha = 1
      }
    }

    context.fillStyle = palette.ground
    context.fillRect(0, height - groundHeight, width, groundHeight)
    context.fillStyle = palette.groundTop
    context.fillRect(0, height - groundHeight, width, 3)
    context.globalAlpha = 0.22
    context.fillStyle = palette.foliageLight
    for (let x = -(sceneOffset % 28); x < width + 28; x += 28) context.fillRect(x, height - groundHeight + 9, 16, 2)
    context.globalAlpha = 1
  }

  const drawPipe = (pipe) => {
    const pipeWidth = 48
    const capHeight = 11
    const gapTop = pipe.opening - pipe.gap / 2
    const gapBottom = pipe.opening + pipe.gap / 2
    context.fillStyle = palette.pipeDark
    context.fillRect(pipe.x, 0, pipeWidth, gapTop)
    context.fillRect(pipe.x, gapBottom, pipeWidth, height - groundHeight - gapBottom)
    context.fillStyle = palette.pipe
    context.fillRect(pipe.x + 5, 0, pipeWidth - 10, gapTop)
    context.fillRect(pipe.x + 5, gapBottom, pipeWidth - 10, height - groundHeight - gapBottom)
    context.fillStyle = palette.pipeLight
    context.fillRect(pipe.x + 9, 0, 5, gapTop)
    context.fillRect(pipe.x + 9, gapBottom, 5, height - groundHeight - gapBottom)
    context.fillStyle = palette.pipeEdge
    context.fillRect(pipe.x - 5, gapTop - capHeight, pipeWidth + 10, capHeight)
    context.fillRect(pipe.x - 5, gapBottom, pipeWidth + 10, capHeight)
  }

  const drawFireworks = () => {
    if (!fireworks.length) return

    context.save()
    context.lineCap = "round"
    for (const particle of fireworks) {
      const elapsed = particle.age - particle.delay
      if (elapsed < 0) continue

      const progress = elapsed / particle.life
      const ignition = Math.min(1, progress / 0.07)
      const fade = Math.pow(1 - progress, 1.25)
      const twinkle = 0.82 + Math.sin(elapsed * 24 + particle.twinkle) * 0.18
      context.globalAlpha = ignition * fade * twinkle * 0.88
      context.lineWidth = particle.size
      context.strokeStyle = particle.color
      context.beginPath()
      context.moveTo(particle.x, particle.y)
      context.lineTo(particle.x - particle.velocityX * 0.075, particle.y - particle.velocityY * 0.075)
      context.stroke()
      context.fillStyle = particle.color
      context.beginPath()
      context.arc(particle.x, particle.y, particle.reducedMotion ? 1.8 : Math.max(0.65, particle.size * fade), 0, Math.PI * 2)
      context.fill()
    }
    context.restore()
  }

  const drawBirdShape = (targetContext, x, y, tilt, wing, scale = 1, facing = 1, options = {}) => {
    const perched = wing === null
    const gliding = typeof wing === "number" && wing < 0
    const wingPose = perched || gliding ? 0.5 : clamp(wing, 0, 1)
    const headPitch = clamp(options.headPitch || 0, -0.28, 0.28)
    const tailFan = clamp(options.tailFan || 0, 0, 1)
    const bodyLift = perched ? 0 : Math.sin((options.wingPhase || 0) + 0.7) * 0.55

    targetContext.save()
    targetContext.translate(x, y + bodyLift)
    targetContext.rotate(tilt)
    targetContext.scale(facing * scale, scale)
    targetContext.lineJoin = "round"
    targetContext.lineCap = "round"

    // Legs sit behind the body and only drop into a weight-bearing pose while perched.
    if (perched) {
      targetContext.strokeStyle = palette.birdLeg
      targetContext.lineWidth = 1.05
      for (const legX of [-3.2, 3.4]) {
        targetContext.beginPath()
        targetContext.moveTo(legX, 7.2)
        targetContext.lineTo(legX + 0.2, 13.5)
        targetContext.lineTo(legX - 1.1, 15.6)
        targetContext.stroke()

        targetContext.beginPath()
        targetContext.moveTo(legX - 1.1, 15.5)
        targetContext.quadraticCurveTo(legX - 4.4, 16.2, legX - 5.2, 15.5)
        targetContext.moveTo(legX - 1.0, 15.5)
        targetContext.quadraticCurveTo(legX + 2.4, 16.7, legX + 4.2, 15.8)
        targetContext.moveTo(legX - 0.8, 15.1)
        targetContext.quadraticCurveTo(legX + 0.2, 17.3, legX + 1.5, 17.5)
        targetContext.stroke()
      }
    }

    // Three overlapping rectrices make the tail read as feathers instead of one polygon.
    const tailSpread = 2.2 + tailFan * 3.8
    targetContext.fillStyle = palette.birdDark
    for (let feather = -1; feather <= 1; feather += 1) {
      const offset = feather * tailSpread
      targetContext.beginPath()
      targetContext.moveTo(-11.5, -1 + feather * 1.5)
      targetContext.lineTo(-29.5, offset - 3.3)
      targetContext.quadraticCurveTo(-31, offset, -28.6, offset + 2.5)
      targetContext.lineTo(-10.5, 4.2 + feather * 0.7)
      targetContext.closePath()
      targetContext.fill()
    }
    targetContext.fillStyle = palette.birdFeather
    targetContext.globalAlpha = 0.72
    targetContext.beginPath()
    targetContext.moveTo(-13, 0)
    targetContext.lineTo(-27, -1.8)
    targetContext.lineTo(-19, 4.5)
    targetContext.closePath()
    targetContext.fill()
    targetContext.globalAlpha = 1

    // Torso: a tapered dorsal line, rounded breast and narrower rump give a sparrow-like anatomy.
    targetContext.fillStyle = palette.birdBack
    targetContext.beginPath()
    targetContext.moveTo(-13.5, -3.5)
    targetContext.bezierCurveTo(-8.5, -9.7, 2.5, -10.2, 10.4, -5.3)
    targetContext.bezierCurveTo(15.3, -2.2, 14.3, 5.6, 7.3, 9.2)
    targetContext.bezierCurveTo(-0.8, 13.6, -10.5, 8.7, -14.6, 3.2)
    targetContext.bezierCurveTo(-16, 1, -15.5, -1.7, -13.5, -3.5)
    targetContext.closePath()
    targetContext.fill()

    targetContext.fillStyle = palette.birdBody
    targetContext.beginPath()
    targetContext.moveTo(-9.5, -5.1)
    targetContext.bezierCurveTo(-1.5, -7.8, 7.4, -6.9, 11.5, -2)
    targetContext.bezierCurveTo(14.5, 1.5, 11.7, 8.2, 5, 10.1)
    targetContext.bezierCurveTo(-2.8, 12.2, -10.5, 7.2, -12.4, 2.4)
    targetContext.bezierCurveTo(-13.3, 0, -12.1, -3.8, -9.5, -5.1)
    targetContext.fill()

    targetContext.fillStyle = palette.birdChest
    targetContext.beginPath()
    targetContext.ellipse(5.4, 4.3, 8.1, 5.9, -0.18, -0.2, Math.PI * 1.08)
    targetContext.fill()
    targetContext.fillStyle = palette.birdLight
    targetContext.globalAlpha = 0.42
    targetContext.beginPath()
    targetContext.ellipse(7.4, 3.7, 4.8, 3.5, -0.22, -0.4, Math.PI * 1.1)
    targetContext.fill()
    targetContext.globalAlpha = 1

    // Wing grows from the shoulder/scapular area. During a glide the primaries stay splayed.
    targetContext.fillStyle = palette.birdWing
    targetContext.strokeStyle = palette.birdFeather
    targetContext.lineWidth = 0.8
    if (perched) {
      targetContext.beginPath()
      targetContext.moveTo(-7.8, -3.1)
      targetContext.bezierCurveTo(-2.5, -5.6, 6.5, -1.4, 4.6, 3.8)
      targetContext.bezierCurveTo(1.5, 7.9, -8.8, 7.4, -11.1, 2.2)
      targetContext.bezierCurveTo(-12.2, -0.2, -10.5, -2.3, -7.8, -3.1)
      targetContext.fill()
    } else if (gliding) {
      targetContext.beginPath()
      targetContext.moveTo(-7.5, -2.5)
      targetContext.bezierCurveTo(-10.8, -8.5, -24.8, -9.8, -31.5, -5.2)
      targetContext.bezierCurveTo(-25, -2.8, -20.8, 1.2, -17.4, 5.3)
      targetContext.bezierCurveTo(-9.2, 6.7, -1.4, 4.5, 4.8, -1.5)
      targetContext.closePath()
      targetContext.fill()
      targetContext.globalAlpha = 0.62
      for (let feather = 0; feather < 4; feather += 1) {
        targetContext.beginPath()
        targetContext.moveTo(-17 + feather * 2.3, -3.2 + feather * 0.65)
        targetContext.lineTo(-31 + feather * 3.5, -5.4 + feather * 2.1)
        targetContext.stroke()
      }
      targetContext.globalAlpha = 1
    } else {
      const up = wingPose
      const tipX = -18 - Math.sin(up * Math.PI) * 5.5
      const tipY = 11.5 - up * 34
      const wristX = -11.5 - Math.sin(up * Math.PI) * 4
      const wristY = 5.5 - up * 23
      targetContext.beginPath()
      targetContext.moveTo(-7.4, -2.4)
      targetContext.bezierCurveTo(-11.2, -4.4 - up * 3, wristX - 4, wristY, tipX, tipY)
      targetContext.bezierCurveTo(tipX + 5.2, tipY + 1.8, wristX + 5.3, wristY + 4.4, 3.8, -1.8)
      targetContext.quadraticCurveTo(0.5, 5.9, -8.6, 5.8)
      targetContext.closePath()
      targetContext.fill()

      targetContext.globalAlpha = 0.5
      targetContext.strokeStyle = palette.birdLight
      for (let feather = 0; feather < 4; feather += 1) {
        const f = feather / 3
        targetContext.beginPath()
        targetContext.moveTo(-7 + feather * 1.8, -0.2)
        targetContext.quadraticCurveTo(wristX - 2 + feather * 1.3, wristY + feather * 1.6, tipX + feather * 3.2, tipY + 3.4 + feather * 1.8)
        targetContext.stroke()
      }
      targetContext.globalAlpha = 1
    }

    // Scapulars visually lock the wing to the torso.
    targetContext.fillStyle = palette.birdBack
    targetContext.globalAlpha = 0.82
    targetContext.beginPath()
    targetContext.ellipse(-4.5, -2.7, 6.7, 3.2, -0.2, 0, TAU)
    targetContext.fill()
    targetContext.globalAlpha = 1

    // Neck and head are slightly decoupled from body pitch, as in real flight stabilization.
    targetContext.save()
    targetContext.translate(9.5, -4.6)
    targetContext.rotate(headPitch)
    targetContext.fillStyle = palette.birdBody
    targetContext.beginPath()
    targetContext.ellipse(2.2, -0.4, 8.1, 7.1, -0.08, 0, TAU)
    targetContext.fill()

    targetContext.fillStyle = palette.birdCrown
    targetContext.beginPath()
    targetContext.ellipse(1, -4.2, 7.1, 3.4, -0.1, Math.PI, TAU)
    targetContext.fill()

    targetContext.fillStyle = palette.birdLight
    targetContext.beginPath()
    targetContext.ellipse(4.5, 1.5, 5.5, 4.3, -0.24, -0.4, Math.PI * 1.35)
    targetContext.fill()

    // Subtle cheek/auricular patch helps the head read anatomically at very small sizes.
    targetContext.fillStyle = palette.birdCrown
    targetContext.globalAlpha = 0.58
    targetContext.beginPath()
    targetContext.ellipse(0.2, 0.7, 3.1, 2.5, -0.15, 0, TAU)
    targetContext.fill()
    targetContext.globalAlpha = 1

    targetContext.fillStyle = palette.beak
    targetContext.beginPath()
    targetContext.moveTo(9.1, -1.8)
    targetContext.lineTo(18.2, 0.25)
    targetContext.lineTo(9, 2.2)
    targetContext.quadraticCurveTo(10.3, 0.15, 9.1, -1.8)
    targetContext.fill()
    targetContext.strokeStyle = palette.birdDark
    targetContext.globalAlpha = 0.5
    targetContext.beginPath()
    targetContext.moveTo(9.6, 0.15)
    targetContext.lineTo(16.5, 0.35)
    targetContext.stroke()
    targetContext.globalAlpha = 1

    targetContext.fillStyle = palette.eye
    targetContext.beginPath()
    targetContext.arc(5.6, -3.1, 2.1, 0, TAU)
    targetContext.fill()
    targetContext.fillStyle = palette.pupil
    targetContext.beginPath()
    targetContext.arc(6, -3.15, 1.15, 0, TAU)
    targetContext.fill()
    targetContext.fillStyle = palette.eye
    targetContext.beginPath()
    targetContext.arc(6.35, -3.55, 0.34, 0, TAU)
    targetContext.fill()
    targetContext.restore()

    targetContext.restore()
  }

  const startFreedomFlight = (startPoint) => {
    const flightCanvas = document.createElement("canvas")
    flightCanvas.setAttribute("aria-hidden", "true")
    Object.assign(flightCanvas.style, {
      position: "fixed",
      inset: "0",
      width: "100vw",
      height: "100vh",
      pointerEvents: "none",
      zIndex: "10000",
    })
    document.body.append(flightCanvas)

    const flightContext = flightCanvas.getContext("2d", { alpha: true, desynchronized: true })
    const reducedMotion = prefersReducedMotion.matches
    const randomRange = (minimum, maximum) => minimum + Math.random() * (maximum - minimum)
    const ignoredTags = new Set(["SCRIPT", "STYLE", "LINK", "META", "NOSCRIPT", "TEMPLATE", "BR", "WBR", "OPTION", "SVG", "PATH"])

    let viewportWidth = Math.max(1, window.innerWidth)
    let viewportHeight = Math.max(1, window.innerHeight)
    let flightRatio = 1
    let flightFrame = 0
    let destroyed = false
    let suspended = document.hidden
    let previousFlightTime = 0
    let lastPerchSearchAt = -Infinity
    let lastScrollAt = -Infinity
    let lastScrollY = window.scrollY
    let scrollDirection = 0
    let scrollActiveUntil = -Infinity
    let lastScrollRetargetAt = -Infinity
    let perchElement = null
    let lastSelectedPerchElement = null
    let perchAnchor = 0.5
    let perchUntil = 0
    let perchRevision = 0
    let targetRevision = -1
    let smoothedPerchPoint = null

    const resizeFlightCanvas = () => {
      viewportWidth = Math.max(1, window.innerWidth)
      viewportHeight = Math.max(1, window.innerHeight)
      flightRatio = Math.min(window.devicePixelRatio || 1, viewportWidth * viewportHeight > 1000000 ? 1.1 : 1.35)
      flightCanvas.width = Math.round(viewportWidth * flightRatio)
      flightCanvas.height = Math.round(viewportHeight * flightRatio)
      flightContext.setTransform(flightRatio, 0, 0, flightRatio, 0, 0)
      perchRevision += 1
    }
    resizeFlightCanvas()

    const randomFlightPoint = (upperBias = false) => ({
      x: randomRange(46, Math.max(47, viewportWidth - 46)),
      y: randomRange(upperBias ? 54 : 72, Math.max(73, viewportHeight * (upperBias ? 0.46 : 0.7))),
    })

    const isRectInViewport = (rect, margin = 0) =>
      rect.bottom >= -margin && rect.top <= viewportHeight + margin && rect.right >= -margin && rect.left <= viewportWidth + margin

    const isPerchUsable = (element, rect = element?.getBoundingClientRect()) => {
      if (
        !(element instanceof HTMLElement) ||
        !element.isConnected ||
        element === document.body ||
        host.contains(element) ||
        element === flightCanvas
      )
        return false
      if (ignoredTags.has(element.tagName) || !rect) return false
      if (rect.width < 26 || rect.width > viewportWidth * 0.985 || rect.height < 7 || rect.height > viewportHeight * 0.78) return false
      if (!isRectInViewport(rect, 8) || rect.top < 34 || rect.top > viewportHeight - 26 || rect.right < 16 || rect.left > viewportWidth - 16)
        return false

      const style = window.getComputedStyle(element)
      if (style.display === "none" || style.visibility === "hidden" || Number.parseFloat(style.opacity || "1") <= 0.02) return false
      return true
    }

    const discoverPerch = (time = performance.now()) => {
      const candidates = []
      const candidateElements = document.body?.querySelectorAll("*") || []
      const scrollFreshness = clamp(1 - (time - lastScrollAt) / 1800, 0, 1)
      const focusY = viewportHeight * (scrollFreshness > 0 ? (scrollDirection > 0 ? 0.58 : scrollDirection < 0 ? 0.36 : 0.48) : 0.48)
      const focusX = viewportWidth * 0.5

      for (const element of candidateElements) {
        if (!(element instanceof HTMLElement)) continue
        const rect = element.getBoundingClientRect()
        if (!isPerchUsable(element, rect)) continue

        const style = window.getComputedStyle(element)
        const anchor = randomRange(0.18, 0.82)
        const sampleX = clamp(rect.left + rect.width * anchor, 2, viewportWidth - 2)
        const sampleY = clamp(rect.top + Math.min(3, rect.height * 0.22), 2, viewportHeight - 2)
        const frontElements = document.elementsFromPoint(sampleX, sampleY)
        const isActuallyVisible = frontElements.some(
          (front) => front === element || element.contains(front) || (front instanceof Element && front.contains(element)),
        )
        if (!isActuallyVisible) continue

        const tag = element.tagName
        const semanticBonus =
          tag === "BUTTON" || tag === "IMG" || tag === "HR"
            ? 36
            : tag === "ARTICLE" || tag === "SECTION" || tag === "NAV" || tag === "FOOTER" || tag === "ASIDE"
              ? 28
              : tag === "H1" || tag === "H2" || tag === "H3" || tag === "H4" || tag === "P" || tag === "LI"
                ? 21
                : tag === "A" || tag === "STRONG" || tag === "SPAN"
                  ? 15
                  : 8
        const borderBonus =
          (Number.parseFloat(style.borderTopWidth) > 0 && style.borderTopStyle !== "none") ||
          (Number.parseFloat(style.borderBottomWidth) > 0 && style.borderBottomStyle !== "none")
            ? 24
            : 0
        const widthFitness = 28 - Math.min(28, Math.abs(Math.min(rect.width, 440) - 180) * 0.075)
        const candidateX = rect.left + rect.width * anchor
        const centerDistance = Math.hypot((candidateX - focusX) * 0.55, rect.top - focusY)
        const viewportFocusBonus = 34 - Math.min(34, centerDistance * 0.07)
        const recentScrollBonus = scrollFreshness * (32 - Math.min(32, Math.abs(rect.top - focusY) * 0.075))
        const positioningPenalty = style.position === "fixed" || style.position === "sticky" ? 10 : 0

        candidates.push({
          element,
          anchor,
          score: semanticBonus + borderBonus + widthFitness + viewportFocusBonus + recentScrollBonus - positioningPenalty + Math.random() * 10,
        })
      }

      if (!candidates.length) return false
      candidates.sort((a, b) => b.score - a.score)
      const shortlist = candidates.slice(0, Math.min(14, candidates.length))
      const freshShortlist = shortlist.filter((candidate) => candidate.element !== lastSelectedPerchElement)
      const selectionPool = freshShortlist.length ? freshShortlist : shortlist
      const selected = selectionPool[Math.floor(Math.random() * Math.min(7, selectionPool.length))]
      perchElement = selected.element
      lastSelectedPerchElement = selected.element
      perchAnchor = selected.anchor
      lastPerchSearchAt = time
      targetRevision = perchRevision
      return true
    }

    const invalidatePerch = () => {
      perchElement = null
      targetRevision = -1
      smoothedPerchPoint = null
    }

    const ensurePerch = (time = performance.now(), force = false) => {
      if (!force && perchElement?.isConnected && isPerchUsable(perchElement)) return true
      if (!force && time - lastPerchSearchAt < 180) return false
      return discoverPerch(time)
    }

    const getPerchPoint = (time) => {
      if (!ensurePerch(time)) return null
      const rect = perchElement.getBoundingClientRect()
      if (!isPerchUsable(perchElement, rect)) {
        invalidatePerch()
        return null
      }
      return {
        x: clamp(rect.left + rect.width * perchAnchor, 28, viewportWidth - 28),
        y: clamp(rect.top - 14, 38, viewportHeight - 34),
      }
    }

    // DOM geometry can move by several pixels between frames because of font
    // rendering, responsive layout, sticky content or scroll. Never feed those
    // jumps directly into the flight controller: move the perceived perch target
    // continuously instead. This makes the final approach immune to layout snaps.
    const trackPerchPoint = (rawPoint, delta, responsiveness = 11, maxSpeed = 260) => {
      if (!rawPoint) {
        smoothedPerchPoint = null
        return null
      }
      if (!smoothedPerchPoint) {
        smoothedPerchPoint = { x: rawPoint.x, y: rawPoint.y }
        return smoothedPerchPoint
      }

      const dx = rawPoint.x - smoothedPerchPoint.x
      const dy = rawPoint.y - smoothedPerchPoint.y
      const distance = Math.hypot(dx, dy)
      if (distance < 0.001) return smoothedPerchPoint

      const blend = 1 - Math.exp(-responsiveness * Math.max(delta, 0.001))
      const wantedStep = distance * blend
      const maxStep = Math.max(0.5, maxSpeed * Math.max(delta, 0.001))
      const step = Math.min(wantedStep, maxStep)
      smoothedPerchPoint.x += (dx / distance) * step
      smoothedPerchPoint.y += (dy / distance) * step
      return smoothedPerchPoint
    }

    const position = {
      x: clamp(startPoint.x, 28, viewportWidth - 28),
      y: clamp(startPoint.y, 44, viewportHeight - 36),
    }
    const initialFacing = Math.random() < 0.5 ? -1 : 1
    const velocity = { x: initialFacing * randomRange(75, 120), y: randomRange(-190, -145) }
    let facing = initialFacing
    let wingPhase = Math.random() * TAU
    let state = "launch"
    let stateStartedAt = 0
    let roamTarget = randomFlightPoint(true)
    let roamUntil = 0
    let nextPerchSearchAt = 0
    const wanderSeed = Math.random() * 1000

    const setState = (nextState, time) => {
      state = nextState
      stateStartedAt = time
    }

    const chooseRoamTarget = (time, upperBias = false) => {
      roamTarget = randomFlightPoint(upperBias)
      roamUntil = time + randomRange(reducedMotion ? 500 : 900, reducedMotion ? 900 : 1900)
    }

    const steerToward = (target, delta, time, settings = {}) => {
      const dx = target.x - position.x
      const dy = target.y - position.y
      const distance = Math.max(0.001, Math.hypot(dx, dy))
      const arrivalRadius = settings.arrivalRadius ?? 80
      const cruiseSpeed = settings.speed ?? 190
      const minimumSpeed = settings.minimumSpeed ?? 70
      const arrivalScale = clamp(distance / arrivalRadius, 0, 1)
      const desiredSpeed = minimumSpeed + (cruiseSpeed - minimumSpeed) * smoothstep(arrivalScale)
      let desiredX = (dx / distance) * desiredSpeed
      let desiredY = (dy / distance) * desiredSpeed

      if (settings.wander) {
        const normalX = -dy / distance
        const normalY = dx / distance
        const wander = (Math.sin(time * 0.0017 + wanderSeed) + Math.sin(time * 0.0031 + wanderSeed * 0.37) * 0.45) * settings.wander
        desiredX += normalX * wander
        desiredY += normalY * wander
      }

      let accelerationX = desiredX - velocity.x
      let accelerationY = desiredY - velocity.y
      const accelerationLength = Math.max(0.001, Math.hypot(accelerationX, accelerationY))
      const maxAcceleration = settings.acceleration || 430
      if (accelerationLength > maxAcceleration) {
        accelerationX = (accelerationX / accelerationLength) * maxAcceleration
        accelerationY = (accelerationY / accelerationLength) * maxAcceleration
      }

      velocity.x += accelerationX * delta
      velocity.y += accelerationY * delta
      position.x += velocity.x * delta
      position.y += velocity.y * delta
      return distance
    }

    const keepInsideViewport = (delta) => {
      const marginX = 22
      const marginTop = 38
      const marginBottom = 30
      const right = viewportWidth - marginX
      const bottom = viewportHeight - marginBottom
      const boundaryForce = 24

      if (position.x < marginX) velocity.x += (marginX - position.x) * boundaryForce * delta
      else if (position.x > right) velocity.x -= (position.x - right) * boundaryForce * delta

      if (position.y < marginTop) velocity.y += (marginTop - position.y) * boundaryForce * delta
      else if (position.y > bottom) velocity.y -= (position.y - bottom) * boundaryForce * delta

      // Catastrophic recovery only if a resize/layout discontinuity placed the
      // canvas sprite far outside the viewport. Normal flight never hits this.
      position.x = clamp(position.x, -48, viewportWidth + 48)
      position.y = clamp(position.y, -48, viewportHeight + 48)
    }

    const animateFlight = (time) => {
      if (destroyed || suspended) return
      if (!stateStartedAt) {
        stateStartedAt = time
        chooseRoamTarget(time, true)
        nextPerchSearchAt = time + randomRange(700, 1300)
      }

      const delta = Math.min((time - previousFlightTime) / 1000 || 0, 0.034)
      previousFlightTime = time
      flightContext.clearRect(0, 0, viewportWidth, viewportHeight)

      let gliding = false
      let perched = false
      let distance = Infinity
      const scrolling = time < scrollActiveUntil

      if (state === "launch") {
        distance = steerToward(roamTarget, delta, time, { speed: 225, minimumSpeed: 130, acceleration: 560, wander: 14 })
        if (time - stateStartedAt > 700 || distance < 65) {
          chooseRoamTarget(time)
          setState("roam", time)
        }
      } else if (state === "roam") {
        distance = steerToward(roamTarget, delta, time, {
          speed: reducedMotion ? 145 : 192,
          minimumSpeed: reducedMotion ? 86 : 96,
          acceleration: reducedMotion ? 300 : 390,
          wander: reducedMotion ? 7 : 34,
          arrivalRadius: 110,
        })
        gliding =
          !reducedMotion && Math.hypot(velocity.x, velocity.y) > 145 && Math.abs(velocity.y) < 85 && Math.sin(time * 0.0015 + wanderSeed) > 0.4

        if (distance < 46 || time >= roamUntil) chooseRoamTarget(time)
        if (time >= nextPerchSearchAt) {
          invalidatePerch()
          if (ensurePerch(time, true)) {
            setState("approach", time)
            if (scrolling) lastScrollRetargetAt = time
          } else {
            nextPerchSearchAt = time + (scrolling ? 160 : 500)
          }
        }
      } else if (state === "approach") {
        // While the page is moving, keep reconsidering the landing target from
        // the currently visible viewport. We intentionally never allow touchdown
        // until scrolling has been quiet for a short settling window.
        if (scrolling && time - lastScrollRetargetAt >= 150) {
          invalidatePerch()
          ensurePerch(time, true)
          lastScrollRetargetAt = time
        }

        const rawTarget = getPerchPoint(time)
        const target = rawTarget ? trackPerchPoint(rawTarget, delta, 10, 235) : null
        if (!target) {
          chooseRoamTarget(time)
          nextPerchSearchAt = time + 350
          setState("roam", time)
        } else {
          const dx = target.x - position.x
          const dy = target.y - position.y
          const beforeDistance = Math.hypot(dx, dy)

          // The last part of a landing is a flare, not a snap. Far away we use
          // the normal steering model; close to the perch we switch to a
          // critically damped proportional controller so speed naturally tends
          // to zero at the exact contact point.
          if (beforeDistance > 58) {
            distance = steerToward(target, delta, time, {
              speed: 132,
              minimumSpeed: 34,
              acceleration: 330,
              arrivalRadius: 180,
            })
          } else {
            const landingGain = reducedMotion ? 5.2 : 4.4
            const desiredX = dx * landingGain
            const desiredY = dy * landingGain
            const desiredLength = Math.hypot(desiredX, desiredY)
            const maxLandingSpeed = reducedMotion ? 76 : 92
            const speedScale = desiredLength > maxLandingSpeed ? maxLandingSpeed / desiredLength : 1
            const targetVelocityX = desiredX * speedScale
            const targetVelocityY = desiredY * speedScale
            const damping = 1 - Math.exp(-(reducedMotion ? 15 : 12) * delta)

            velocity.x += (targetVelocityX - velocity.x) * damping
            velocity.y += (targetVelocityY - velocity.y) * damping
            position.x += velocity.x * delta
            position.y += velocity.y * delta
            distance = Math.hypot(target.x - position.x, target.y - position.y)
          }

          const approachAge = time - stateStartedAt
          const approachSpeed = Math.hypot(velocity.x, velocity.y)
          const canLand = !scrolling && distance < 0.65 && approachSpeed < 10

          if (canLand) {
            // Do not snap to the perch. The bird enters its resting state from
            // the exact position reached by the flight integrator; the resting
            // spring below settles the remaining sub-pixel error continuously.
            perchUntil = time + randomRange(reducedMotion ? 2600 : 3100, reducedMotion ? 3800 : 5200)
            setState("perched", time)
          } else if (approachAge > 7500 && distance > 42) {
            // If layout movement made this approach awkward, abandon it and
            // pick another visible perch instead of teleporting to finish.
            invalidatePerch()
            chooseRoamTarget(time)
            nextPerchSearchAt = time + 180
            setState("roam", time)
          }
        }
      } else if (state === "perched") {
        perched = true
        const rawTracked = getPerchPoint(time)
        const tracked = rawTracked ? trackPerchPoint(rawTracked, delta, 14, 420) : null
        if (!tracked) {
          velocity.x = facing * randomRange(72, 108)
          velocity.y = randomRange(-190, -145)
          chooseRoamTarget(time, true)
          nextPerchSearchAt = time + randomRange(800, 1500)
          setState("takeoff", time)
        } else {
          const takeoffAnticipation = clamp((time - (perchUntil - 500)) / 500, 0, 1)
          const restTargetX = tracked.x + Math.sin(time * 0.0037 + wanderSeed) * 0.28
          const restTargetY = tracked.y + Math.sin(time * 0.0059 + wanderSeed * 0.41) * 0.18 + takeoffAnticipation * 0.85

          // Resting uses a damped spring instead of assigning DOM coordinates
          // directly. There is therefore no frame in which the bird can teleport
          // when the target element shifts or the landing state changes.
          const restDx = restTargetX - position.x
          const restDy = restTargetY - position.y
          const spring = reducedMotion ? 50 : 64
          const damping = reducedMotion ? 13 : 15
          velocity.x += (restDx * spring - velocity.x * damping) * delta
          velocity.y += (restDy * spring - velocity.y * damping) * delta
          position.x += velocity.x * delta
          position.y += velocity.y * delta

          if (time >= perchUntil) {
            facing = Math.random() < 0.5 ? -1 : 1
            velocity.x = facing * randomRange(82, 118)
            velocity.y = randomRange(-210, -160)
            chooseRoamTarget(time, true)
            nextPerchSearchAt = time + randomRange(1100, 2300)
            setState("takeoff", time)
          }
        }
      } else if (state === "takeoff") {
        distance = steerToward(roamTarget, delta, time, { speed: 226, minimumSpeed: 138, acceleration: 565, wander: 10 })
        if (time - stateStartedAt > 720 || distance < 62) {
          chooseRoamTarget(time)
          setState("roam", time)
        }
      }

      keepInsideViewport(delta)
      if (Math.abs(velocity.x) > 18) facing = velocity.x < 0 ? -1 : 1
      const speed = Math.hypot(velocity.x, velocity.y)
      const flightAngle = Math.atan2(velocity.y, Math.max(35, Math.abs(velocity.x)))
      const tilt = perched ? 0 : clamp(flightAngle * 0.48, -0.48, 0.42)
      const cadence = perched ? 0 : clamp(13 + speed * 0.028 + Math.max(0, -velocity.y) * 0.018, 13, 22)
      wingPhase = (wingPhase + cadence * delta) % TAU
      const wing = perched ? null : gliding ? -1 : wingPoseFromPhase(wingPhase)
      const perchedHeadMotion = Math.sin(time * 0.0024 + wanderSeed) * 0.105 + Math.sin(time * 0.0068 + wanderSeed * 0.73) * 0.035
      const headPitch = perched ? perchedHeadMotion : clamp(-tilt * 0.46, -0.2, 0.2)
      const tailFan = perched
        ? 0.12 + (Math.sin(time * 0.0031 + wanderSeed) + 1) * 0.025
        : clamp(Math.abs(flightAngle) * 0.9 + (state === "approach" ? 0.45 : 0.08), 0.06, 0.8)
      const perchBob = perched ? Math.sin(time * 0.0048 + wanderSeed * 0.6) * 0.16 : 0
      const restingTilt = perched ? Math.sin(time * 0.0029 + wanderSeed) * 0.018 : tilt

      drawBirdShape(flightContext, position.x, position.y + perchBob, restingTilt, wing, 1.24, facing, {
        headPitch,
        tailFan,
        wingPhase,
      })

      flightFrame = requestAnimationFrame(animateFlight)
    }

    const onScroll = () => {
      const nextScrollY = window.scrollY
      const deltaY = nextScrollY - lastScrollY
      if (Math.abs(deltaY) > 1) scrollDirection = Math.sign(deltaY)
      lastScrollY = nextScrollY
      lastScrollAt = performance.now()
      scrollActiveUntil = lastScrollAt + 360
      perchRevision += 1

      if (state === "perched") {
        // Any real page movement scares the bird off the perch. Never drag a
        // resting bird along with getBoundingClientRect() while the user scrolls.
        velocity.x = facing * randomRange(82, 118)
        velocity.y = randomRange(-215, -165)
        invalidatePerch()
        chooseRoamTarget(lastScrollAt, true)
        nextPerchSearchAt = lastScrollAt + 110
        setState("takeoff", lastScrollAt)
      } else if (state === "approach") {
        // Do not commit to a moving viewport. The animation loop retargets at a
        // throttled cadence while scroll events keep arriving, so the bird keeps
        // searching but cannot finish a landing until the page settles.
        nextPerchSearchAt = Math.min(nextPerchSearchAt, lastScrollAt + 90)
      } else if (state === "roam" || state === "takeoff") {
        // Bias the next search toward whatever section is currently visible.
        nextPerchSearchAt = Math.min(nextPerchSearchAt, lastScrollAt + 140)
      }
    }

    const onResize = () => {
      resizeFlightCanvas()
      position.x = clamp(position.x, 28, viewportWidth - 28)
      position.y = clamp(position.y, 44, viewportHeight - 34)
      invalidatePerch()
      chooseRoamTarget(performance.now())
    }

    const onVisibilityChange = () => {
      suspended = document.hidden
      if (suspended) {
        if (flightFrame) cancelAnimationFrame(flightFrame)
        flightFrame = 0
        return
      }
      if (!flightFrame && !destroyed) {
        previousFlightTime = performance.now()
        flightFrame = requestAnimationFrame(animateFlight)
      }
    }

    window.addEventListener("scroll", onScroll, { passive: true })
    window.addEventListener("resize", onResize, { passive: true })
    document.addEventListener("visibilitychange", onVisibilityChange)

    ensurePerch(performance.now(), true)
    if (!suspended) flightFrame = requestAnimationFrame(animateFlight)

    return {
      destroy() {
        if (destroyed) return
        destroyed = true
        if (flightFrame) cancelAnimationFrame(flightFrame)
        window.removeEventListener("scroll", onScroll)
        window.removeEventListener("resize", onResize)
        document.removeEventListener("visibilitychange", onVisibilityChange)
        flightCanvas.remove()
      },
    }
  }
  const drawBird = () => {
    if (!bird) return
    const tilt = clamp(bird.velocity / 470, -0.42, 0.72)
    drawBirdShape(context, bird.x, bird.y, tilt, wingPoseFromPhase(bird.wingPhase), 1, 1, {
      headPitch: clamp(-tilt * 0.32, -0.16, 0.16),
      tailFan: clamp(Math.abs(bird.velocity) / 620, 0.05, 0.42),
      wingPhase: bird.wingPhase,
    })
  }

  const draw = () => {
    drawBackground()
    drawFireworks()
    for (const pipe of pipes) drawPipe(pipe)
    drawBird()
  }

  const loop = (time) => {
    const delta = Math.min((time - previousTime) / 1000 || 0, 0.032)
    previousTime = time
    update(delta)
    draw()
    frame = isVisible && (state === "ready" || state === "playing" || state === "over" || state === "freed") ? requestAnimationFrame(loop) : 0
  }

  const resume = () => {
    if (frame || (state !== "ready" && state !== "playing" && state !== "over" && state !== "freed") || !isVisible || document.hidden) return
    previousTime = performance.now()
    frame = requestAnimationFrame(loop)
  }

  const pause = () => {
    if (!frame) return
    cancelAnimationFrame(frame)
    frame = 0
  }

  const onPointerDown = () => flap()
  const onOverlayPointerDown = (event) => {
    if (event.target.closest("button")) return
    if (state === "ready") flap()
  }
  const onOverlayClick = (event) => {
    if (!event.target.closest("button")) return
    event.stopPropagation()
    start()
  }
  const onKeyDown = (event) => {
    if (event.key !== " " && event.key !== "ArrowUp") return
    event.preventDefault()
    event.stopPropagation()
    flap()
  }
  const onVisibilityChange = () => (document.hidden ? pause() : resume())

  const resizeObserver = new ResizeObserver(resize)
  const visibilityObserver =
    "IntersectionObserver" in window
      ? new IntersectionObserver(
          ([entry]) => {
            isVisible = entry.isIntersecting
            if (isVisible) resume()
            else pause()
          },
          { threshold: 0.01 },
        )
      : null

  resizeObserver.observe(host)
  visibilityObserver?.observe(host)
  canvas.addEventListener("pointerdown", onPointerDown)
  overlay.addEventListener("pointerdown", onOverlayPointerDown)
  overlay.addEventListener("click", onOverlayClick)
  canvas.addEventListener("keydown", onKeyDown)
  document.addEventListener("visibilitychange", onVisibilityChange)
  reset()
  resize()
  resume()

  return {
    destroy() {
      pause()
      freedomFlight?.destroy()
      freedomFlight = null
      resizeObserver.disconnect()
      visibilityObserver?.disconnect()
      canvas.removeEventListener("pointerdown", onPointerDown)
      overlay.removeEventListener("pointerdown", onOverlayPointerDown)
      overlay.removeEventListener("click", onOverlayClick)
      canvas.removeEventListener("keydown", onKeyDown)
      document.removeEventListener("visibilitychange", onVisibilityChange)
      host.replaceChildren()
    },
  }
}
