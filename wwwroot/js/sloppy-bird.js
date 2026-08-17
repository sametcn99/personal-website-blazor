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

const randomFrom = (seed) => {
  const value = Math.sin(seed * 12.9898) * 43758.5453
  return value - Math.floor(value)
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
  const context = canvas.getContext("2d", { alpha: false })
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
  let isVisible = true
  let bird
  let pipes = []
  let buildings = []
  let shrubs = []
  let fireworks = []
  let freedomFlight = null
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
    bird = { x: Math.max(88, width * 0.22), y: Math.max(70, (height - groundHeight) * 0.47), velocity: 0, radius: 12, wingPhase: 0 }
    pipes.length = 0
    fireworks.length = 0
    score = 0
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

  const showGameOver = () => {
    if (state !== "playing") return
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
    pipes.length = 0
    fireworks.length = 0
    scoreHud.hidden = true
    overlay.hidden = true
    bird = null

    freedomFlight = startFreedomFlight(startPoint, () => {
      freedomFlight = null
      overlay.innerHTML = `<strong><span class="sloppy-bird-title-word">Sloppy</span><span class="sloppy-bird-title-accent">Bird</span><span class="sloppy-bird-title-status">is free.</span></strong><span>Thank you for setting Sloppy Bird free.</span><button type="button" data-umami-event="sloppy-bird-play-again-click">Play again</button>`
      overlay.hidden = false
    })
  }

  const start = () => {
    reset()
    state = "playing"
    scoreHud.hidden = false
    overlay.style.cursor = ""
    overlay.hidden = true
    canvas.focus({ preventScroll: true })
  }

  const flap = () => {
    if (state === "ready") start()
    if (state !== "playing") return
    bird.velocity = -292
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
    const backgroundSpeed = state === "playing" ? 30 : 9
    if (!prefersReducedMotion.matches || state === "playing") sceneOffset = (sceneOffset + backgroundSpeed * delta) % Math.max(1, width + 240)
    if (state !== "playing") return

    bird.velocity += 810 * delta
    bird.y += bird.velocity * delta
    bird.wingPhase = (bird.wingPhase + delta * (bird.velocity < 0 ? 27 : 22)) % (Math.PI * 2)

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
      if (overlaps && !insideGap) showGameOver()
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

  const drawBirdShape = (targetContext, x, y, tilt, wing, scale = 1, facing = 1) => {
    targetContext.save()
    targetContext.translate(x, y)
    targetContext.rotate(tilt)
    targetContext.scale(facing * scale, scale)

    if (wing === null) {
      targetContext.strokeStyle = palette.birdLeg
      targetContext.lineWidth = 1.15
      targetContext.lineCap = "round"
      for (const legX of [-3, 4]) {
        targetContext.beginPath()
        targetContext.moveTo(legX, 8)
        targetContext.lineTo(legX + 0.5, 15)
        targetContext.moveTo(legX + 0.5, 15)
        targetContext.lineTo(legX - 3, 16.5)
        targetContext.moveTo(legX + 0.5, 15)
        targetContext.lineTo(legX + 4, 16.2)
        targetContext.stroke()
      }
    }

    targetContext.fillStyle = palette.birdDark
    targetContext.beginPath()
    targetContext.moveTo(-12, -3)
    targetContext.lineTo(-29, -8)
    targetContext.lineTo(-23, -1)
    targetContext.lineTo(-29, 5)
    targetContext.lineTo(-11, 5)
    targetContext.closePath()
    targetContext.fill()

    targetContext.fillStyle = palette.birdFeather
    targetContext.beginPath()
    targetContext.moveTo(-13, 0)
    targetContext.lineTo(-27, -3)
    targetContext.lineTo(-19, 4)
    targetContext.closePath()
    targetContext.fill()

    const bodyGradient = targetContext.createLinearGradient(-12, -8, 12, 10)
    bodyGradient.addColorStop(0, palette.birdBack)
    bodyGradient.addColorStop(0.48, palette.birdBody)
    bodyGradient.addColorStop(1, palette.birdChest)
    targetContext.fillStyle = bodyGradient
    targetContext.beginPath()
    targetContext.ellipse(-1.5, 1.5, 17, 10.5, -0.08, 0, Math.PI * 2)
    targetContext.fill()

    targetContext.fillStyle = palette.birdChest
    targetContext.globalAlpha = 0.78
    targetContext.beginPath()
    targetContext.ellipse(4, 4.2, 10.5, 5.5, -0.12, 0, Math.PI * 2)
    targetContext.fill()
    targetContext.globalAlpha = 1

    targetContext.fillStyle = palette.birdBody
    targetContext.beginPath()
    targetContext.arc(10, -4.5, 8.2, 0, Math.PI * 2)
    targetContext.fill()
    targetContext.fillStyle = palette.birdCrown
    targetContext.beginPath()
    targetContext.ellipse(8.2, -8.2, 7.3, 3.8, -0.08, Math.PI, Math.PI * 2)
    targetContext.fill()
    targetContext.fillStyle = palette.birdLight
    targetContext.beginPath()
    targetContext.ellipse(12, -2.6, 5.3, 4.6, -0.14, 0, Math.PI * 2)
    targetContext.fill()

    targetContext.fillStyle = palette.birdWing
    targetContext.strokeStyle = palette.birdFeather
    if (wing === null) {
      targetContext.beginPath()
      targetContext.ellipse(-4, 2, 9.5, 5.5, -0.24, 0, Math.PI * 2)
      targetContext.fill()
      targetContext.fillStyle = palette.birdBack
      targetContext.globalAlpha = 0.72
      targetContext.beginPath()
      targetContext.ellipse(-5.5, -0.2, 6.2, 3.2, -0.28, 0, Math.PI * 2)
      targetContext.fill()
      targetContext.globalAlpha = 0.42
      targetContext.strokeStyle = palette.birdLight
      targetContext.beginPath()
      targetContext.moveTo(-10, 1)
      targetContext.quadraticCurveTo(-4, 2, 3, 5)
      targetContext.moveTo(-9, 3)
      targetContext.quadraticCurveTo(-3, 4, 2, 6)
      targetContext.stroke()
      targetContext.globalAlpha = 1
    } else {
      const wingPose = Math.max(0, Math.min(1, wing))
      const wingSpread = Math.sin(wingPose * Math.PI)
      const wingTipX = -14 - wingSpread * 5
      const wingTipY = 11 - wingPose * 31
      const wingShoulderY = -1 - wingPose * 4
      targetContext.beginPath()
      targetContext.moveTo(-7, 1)
      targetContext.bezierCurveTo(-12, wingShoulderY, wingTipX - 5, wingTipY + 4, wingTipX, wingTipY)
      targetContext.bezierCurveTo(wingTipX + 6, wingTipY + 3, 1, -8 + wingPose * 4, 6, -2)
      targetContext.quadraticCurveTo(2, 6, -7, 7)
      targetContext.closePath()
      targetContext.fill()

      targetContext.fillStyle = palette.birdBack
      targetContext.globalAlpha = 0.62
      targetContext.beginPath()
      targetContext.moveTo(-6, 0)
      targetContext.quadraticCurveTo(-9, wingShoulderY, wingTipX + 2, wingTipY + 5)
      targetContext.quadraticCurveTo(-2, -5 + wingPose * 3, 4, -2)
      targetContext.closePath()
      targetContext.fill()

      targetContext.globalAlpha = 0.46
      targetContext.strokeStyle = palette.birdLight
      targetContext.lineWidth = 0.9
      for (let feather = 0; feather < 3; feather += 1) {
        const featherOffset = feather * 0.19
        targetContext.beginPath()
        targetContext.moveTo(-5 + feather * 2.2, 1)
        targetContext.quadraticCurveTo(
          wingTipX * (0.64 + featherOffset),
          wingTipY * (0.58 + featherOffset),
          wingTipX + 2.5 + feather * 3,
          wingTipY + 4 + feather * 2.4,
        )
        targetContext.stroke()
      }
      targetContext.globalAlpha = 1
    }

    targetContext.fillStyle = palette.birdCrown
    targetContext.globalAlpha = 0.7
    targetContext.beginPath()
    targetContext.moveTo(5, -2)
    targetContext.quadraticCurveTo(8, 2, 13, 3)
    targetContext.quadraticCurveTo(8, 5, 4, 2)
    targetContext.closePath()
    targetContext.fill()
    targetContext.globalAlpha = 1

    targetContext.fillStyle = palette.beak
    targetContext.beginPath()
    targetContext.moveTo(16, -4.5)
    targetContext.lineTo(25, -2.2)
    targetContext.lineTo(16, -0.6)
    targetContext.closePath()
    targetContext.fill()
    targetContext.strokeStyle = palette.birdDark
    targetContext.globalAlpha = 0.45
    targetContext.lineWidth = 0.7
    targetContext.beginPath()
    targetContext.moveTo(16.5, -2.6)
    targetContext.lineTo(23, -2.1)
    targetContext.stroke()
    targetContext.globalAlpha = 1

    targetContext.fillStyle = palette.eye
    targetContext.beginPath()
    targetContext.arc(12.6, -6.3, 2.4, 0, Math.PI * 2)
    targetContext.fill()
    targetContext.fillStyle = palette.pupil
    targetContext.beginPath()
    targetContext.arc(13.1, -6.4, 1.35, 0, Math.PI * 2)
    targetContext.fill()
    targetContext.fillStyle = palette.eye
    targetContext.beginPath()
    targetContext.arc(13.55, -6.85, 0.42, 0, Math.PI * 2)
    targetContext.fill()
    targetContext.restore()
  }

  const startFreedomFlight = (startPoint, onComplete) => {
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

    const flightContext = flightCanvas.getContext("2d")
    const viewportWidth = window.innerWidth
    const viewportHeight = window.innerHeight
    const flightRatio = Math.min(window.devicePixelRatio || 1, 1.5)
    flightCanvas.width = Math.round(viewportWidth * flightRatio)
    flightCanvas.height = Math.round(viewportHeight * flightRatio)
    flightContext.setTransform(flightRatio, 0, 0, flightRatio, 0, 0)

    const clamp = (value, minimum, maximum) => Math.max(minimum, Math.min(maximum, value))
    const randomPoint = () => ({
      x: 42 + Math.random() * Math.max(1, viewportWidth - 84),
      y: 58 + Math.random() * Math.max(1, viewportHeight * 0.58),
    })
    const isVisiblePerchElement = (element, minimumWidth = 28) => {
      if (host.contains(element)) return false
      const rect = element.getBoundingClientRect()
      const position = window.getComputedStyle(element).position
      return (
        position !== "fixed" &&
        position !== "sticky" &&
        rect.width > minimumWidth &&
        rect.width < viewportWidth * 0.94 &&
        rect.height > 10 &&
        rect.height < viewportHeight * 0.55 &&
        rect.top > 52 &&
        rect.top < viewportHeight - 36 &&
        rect.right > 20 &&
        rect.left < viewportWidth - 20
      )
    }
    const textPerches = Array.from(document.querySelectorAll("a, button, p, h1, h2, h3, h4, li, strong"))
      .filter((element) => element.textContent?.trim() && isVisiblePerchElement(element))
      .map((element) => ({ element, edge: "top" }))
    const borderedDivPerches = Array.from(document.querySelectorAll("div"))
      .filter((element) => isVisiblePerchElement(element, 56))
      .map((element) => {
        const style = window.getComputedStyle(element)
        const hasTopBorder = style.borderTopStyle !== "none" && Number.parseFloat(style.borderTopWidth) > 0
        const hasBottomBorder = style.borderBottomStyle !== "none" && Number.parseFloat(style.borderBottomWidth) > 0
        if (!hasTopBorder && !hasBottomBorder) return null
        return { element, edge: hasTopBorder ? "top" : "bottom" }
      })
      .filter(Boolean)
    const perchCandidates = [...textPerches, ...borderedDivPerches]
    const perchTarget = perchCandidates.length ? perchCandidates[Math.floor(Math.random() * perchCandidates.length)] : null
    const perchElement = perchTarget?.element || null
    const perchEdge = perchTarget?.edge || "top"
    const perchAnchor = 0.28 + Math.random() * 0.44
    let lastPerchPoint = { x: viewportWidth * (0.25 + Math.random() * 0.5), y: clamp(startPoint.y - 90, 80, viewportHeight * 0.7) }
    const getPerchPoint = () => {
      if (!perchElement?.isConnected) return lastPerchPoint
      const rect = perchElement.getBoundingClientRect()
      lastPerchPoint = { x: rect.left + rect.width * perchAnchor, y: (perchEdge === "bottom" ? rect.bottom : rect.top) - 14 }
      return lastPerchPoint
    }
    const perchPoint = getPerchPoint()
    const exitSide = Math.floor(Math.random() * 3)
    const exitPoint =
      exitSide === 0
        ? { x: 30 + Math.random() * Math.max(1, viewportWidth - 60), y: -54 }
        : exitSide === 1
          ? { x: -54, y: 60 + Math.random() * Math.max(1, viewportHeight * 0.48) }
          : { x: viewportWidth + 54, y: 60 + Math.random() * Math.max(1, viewportHeight * 0.48) }
    const reducedMotion = prefersReducedMotion.matches
    const points = reducedMotion ? [startPoint, exitPoint] : [startPoint, randomPoint(), randomPoint(), perchPoint, randomPoint(), exitPoint]
    const perchSegment = reducedMotion ? -1 : 2
    const segments = []

    for (let index = 0; index < points.length - 1; index += 1) {
      const from = points[index]
      const to = points[index + 1]
      const distance = Math.hypot(to.x - from.x, to.y - from.y)
      const midpointX = (from.x + to.x) * 0.5
      const midpointY = (from.y + to.y) * 0.5
      const bend = (Math.random() - 0.5) * Math.min(190, distance * 0.6)
      const normalX = distance ? -(to.y - from.y) / distance : 0
      const normalY = distance ? (to.x - from.x) / distance : 0
      segments.push({
        from,
        to,
        control: {
          x: clamp(midpointX + normalX * bend, -24, viewportWidth + 24),
          y: clamp(midpointY + normalY * bend - 25 - Math.random() * 38, -24, viewportHeight + 24),
        },
        duration: reducedMotion ? 900 : clamp(distance / 270, 0.72, 1.42) * 1000,
      })
    }

    let flightFrame = 0
    let segmentIndex = 0
    let segmentStartedAt = 0
    let perchUntil = 0
    let facing = 1
    let destroyed = false

    const finish = () => {
      if (destroyed) return
      destroyed = true
      if (flightFrame) cancelAnimationFrame(flightFrame)
      flightCanvas.remove()
      onComplete()
    }

    const animateFlight = (time) => {
      if (destroyed) return
      if (!segmentStartedAt) segmentStartedAt = time
      const segment = segments[segmentIndex]
      if (!segment) {
        finish()
        return
      }

      flightContext.clearRect(0, 0, viewportWidth, viewportHeight)

      if (perchUntil) {
        const trackedPerch = getPerchPoint()
        segment.from.x = trackedPerch.x
        segment.from.y = trackedPerch.y
        const perchBob = Math.sin(time * 0.008) * 0.7
        drawBirdShape(flightContext, segment.from.x, segment.from.y + perchBob, 0, null, 1.22, facing)
        if (time >= perchUntil) {
          perchUntil = 0
          segmentStartedAt = time
          const takeoffDistance = Math.hypot(segment.to.x - segment.from.x, segment.to.y - segment.from.y)
          const takeoffBend = (Math.random() - 0.5) * Math.min(190, takeoffDistance * 0.6)
          const takeoffNormalX = takeoffDistance ? -(segment.to.y - segment.from.y) / takeoffDistance : 0
          const takeoffNormalY = takeoffDistance ? (segment.to.x - segment.from.x) / takeoffDistance : 0
          segment.control.x = clamp((segment.from.x + segment.to.x) * 0.5 + takeoffNormalX * takeoffBend, -24, viewportWidth + 24)
          segment.control.y = clamp(
            (segment.from.y + segment.to.y) * 0.5 + takeoffNormalY * takeoffBend - 25 - Math.random() * 38,
            -24,
            viewportHeight + 24,
          )
          segment.duration = clamp(takeoffDistance / 270, 0.72, 1.42) * 1000
        }
        flightFrame = requestAnimationFrame(animateFlight)
        return
      }

      if (segmentIndex === perchSegment && perchElement) {
        const trackedPerch = getPerchPoint()
        segment.to.x = trackedPerch.x
        segment.to.y = trackedPerch.y
        if (segments[segmentIndex + 1]) {
          segments[segmentIndex + 1].from.x = trackedPerch.x
          segments[segmentIndex + 1].from.y = trackedPerch.y
        }
      }

      const progress = Math.min(1, (time - segmentStartedAt) / segment.duration)
      const inverse = 1 - progress
      const x = inverse * inverse * segment.from.x + 2 * inverse * progress * segment.control.x + progress * progress * segment.to.x
      const y = inverse * inverse * segment.from.y + 2 * inverse * progress * segment.control.y + progress * progress * segment.to.y
      const velocityX = 2 * inverse * (segment.control.x - segment.from.x) + 2 * progress * (segment.to.x - segment.control.x)
      const velocityY = 2 * inverse * (segment.control.y - segment.from.y) + 2 * progress * (segment.to.y - segment.control.y)
      if (Math.abs(velocityX) > 1) facing = velocityX < 0 ? -1 : 1
      const tilt = clamp(Math.atan2(velocityY, Math.max(30, Math.abs(velocityX))) * 0.52, -0.5, 0.5)
      const wing = (Math.sin(time * 0.026 + segmentIndex * 0.7) + 1) * 0.5
      drawBirdShape(flightContext, x, y, tilt, wing, 1.22, facing)

      if (progress >= 1) {
        segmentIndex += 1
        segmentStartedAt = time
        if (segmentIndex - 1 === perchSegment) perchUntil = time + 2200 + Math.random() * 1200
      }
      flightFrame = requestAnimationFrame(animateFlight)
    }

    flightFrame = requestAnimationFrame(animateFlight)
    return {
      destroy() {
        if (destroyed) return
        destroyed = true
        if (flightFrame) cancelAnimationFrame(flightFrame)
        flightCanvas.remove()
      },
    }
  }

  const drawBird = () => {
    if (!bird) return
    const wing = (Math.sin(bird.wingPhase) + 1) * 0.5
    drawBirdShape(context, bird.x, bird.y, Math.max(-0.42, Math.min(0.72, bird.velocity / 470)), wing)
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
    frame = isVisible ? requestAnimationFrame(loop) : 0
  }

  const resume = () => {
    if (frame || !isVisible || document.hidden) return
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
