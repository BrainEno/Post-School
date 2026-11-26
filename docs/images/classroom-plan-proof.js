// Offline plan proof for the prototype classroom. NOT a Unity render.
//
// It reads Assets/Resources/World/classroom_layout.json — the same file ClassroomBuilder.cs consumes —
// and draws a top-down plan plus a front elevation, so the composition can be checked by eye without
// opening the Editor. It also re-derives the constraints ClassroomLayout.Validate enforces, so a layout
// mistake shows up as a reported problem rather than as a picture.
//
// Usage: node docs/images/classroom-plan-proof.js [layout.json] [outDir]
// Dependencies (not committed): npm i @napi-rs/canvas
//   node docs/images/classroom-plan-proof.js \
//     Assets/Resources/World/classroom_layout.json docs/images
const fs = require('fs');
const path = require('path');
const { createCanvas } = require('@napi-rs/canvas');

const data = JSON.parse(fs.readFileSync(
  process.argv[2] || 'Assets/Resources/World/classroom_layout.json', 'utf8'));
const outDir = process.argv[3] || '.';

const C = {
  floor: '#8E8A84', wall: '#DCD3C0', ceiling: '#C4BDB0', wainscot: '#6F6A61',
  board: '#232A26', chalk: '#E9E6DA', wood: '#B08E62', metal: '#4A4E52',
  glass: '#A9C0C4', ink: '#23211F', paper: '#F1EBDC',
  accentRose: '#8E5A54', accentBlue: '#59637A', accentSage: '#71806B', accentOchre: '#C39D59',
  coat: '#7E6A55', trousers: '#3A3E45', skin: '#C9A487', hair: '#2A2730', figureAccent: '#B8442F',
};

const room = data.room;
const seat = data.seating;
const minX = -room.width / 2;
const maxX = room.width / 2;
const minZ = room.centerZ - room.depth / 2;
const maxZ = room.centerZ + room.depth / 2;

// ---------------------------------------------------------------- validation

const problems = [];
const degrees = [];
const seatCount = seat.columns.length * seat.rows.length;
degrees.push(`seat grid ${seat.columns.length} columns x ${seat.rows.length} rows = ${seatCount} seats`);
if (seatCount !== 24) problems.push(`seat count is ${seatCount}, the brief asks for 24`);
if (seatCount !== seat.expectedSeatCount) {
  problems.push(`seat count ${seatCount} disagrees with expectedSeatCount ${seat.expectedSeatCount}`);
}

const alongOrigin = (wall) => (wall === 'front' || wall === 'back' ? minX : minZ);
const wallLength = (wall) => (wall === 'front' || wall === 'back' ? room.width : room.depth);

const openings = [...data.windows.map((w) => ({ ...w, kind: 'window' })), ...data.doors.map((d) => ({ ...d, kind: 'door' }))];
for (const opening of openings) {
  const origin = alongOrigin(opening.wall);
  const length = wallLength(opening.wall);
  const from = opening.centerAlong - opening.width / 2;
  const to = opening.centerAlong + opening.width / 2;
  if (from < 0.3) problems.push(`${opening.id} runs into the low corner of the ${opening.wall} wall`);
  if (to > length - 0.3) problems.push(`${opening.id} runs into the high corner of the ${opening.wall} wall`);
  if (opening.sillHeight + opening.height > room.height - 0.15) problems.push(`${opening.id} is taller than the room`);
  degrees.push(`${opening.kind} ${opening.id}: ${opening.wall} wall, along ${from.toFixed(2)}-${to.toFixed(2)} m (world ${(origin + from).toFixed(2)}-${(origin + to).toFixed(2)})`);

  if (opening.kind === 'door' && (opening.wall === 'front' || opening.wall === 'back')) {
    const worldFrom = origin + from;
    const worldTo = origin + to;
    if (opening.wall === 'back') {
      const boardFrom = -data.blackboard.width / 2;
      const boardTo = data.blackboard.width / 2;
      if (worldFrom < boardTo + 0.3 && worldTo > boardFrom - 0.3) {
        problems.push(`${opening.id} opens onto the blackboard`);
      }
    } else {
      const deskFrom = data.wakeDesk.positionX - data.wakeDesk.width / 2 - 0.4;
      const deskTo = data.wakeDesk.positionX + data.wakeDesk.width / 2 + 0.4;
      if (worldFrom < deskTo && worldTo > deskFrom) problems.push(`${opening.id} opens onto the protagonist's desk`);
    }
  }
}

const byWall = {};
for (const opening of openings) (byWall[opening.wall] ||= []).push(opening);
for (const wall of Object.keys(byWall)) {
  const sorted = byWall[wall].sort((a, b) => a.centerAlong - b.centerAlong);
  for (let i = 1; i < sorted.length; i++) {
    const gap = (sorted[i].centerAlong - sorted[i].width / 2) - (sorted[i - 1].centerAlong + sorted[i - 1].width / 2);
    if (gap < 0.4) problems.push(`${sorted[i - 1].id} and ${sorted[i].id} are only ${gap.toFixed(2)} m apart on the ${wall} wall`);
  }
}

for (const column of seat.columns) {
  if (column - seat.deskWidth / 2 < minX + 0.35) problems.push(`seat column ${column} leaves no aisle against the west wall`);
  if (column + seat.deskWidth / 2 > maxX - 0.35) problems.push(`seat column ${column} leaves no aisle against the east wall`);
}
const deskFront = Math.min(...seat.rows) + seat.chairOffsetZ - seat.chairSize / 2;
const wakeDeskBack = data.wakeDesk.positionZ + data.wakeDesk.depth / 2;
if (deskFront - wakeDeskBack < 0.7 - 1e-4) problems.push(`only ${(deskFront - wakeDeskBack).toFixed(2)} m between the protagonist's desk and the first chair; 0.70 m minimum`);
if (data.wakeDesk.positionZ - data.wakeDesk.depth / 2 - 1.2 < minZ + 0.4) problems.push('protagonist chair is outside the front wall');

const boardFront = Math.min(...seat.rows) - seat.chairSize / 2 - 0.6;
const boardBack = maxZ - 0.1;
if (data.podium.positionZ + data.podium.depth / 2 > boardBack) problems.push('podium is inside the blackboard');
if (data.podium.positionZ - data.podium.depth / 2 - 0.9 - 0.3 < boardFront) degrees.push('podium chair crowds the first row');

for (const light of data.ceilingLights) {
  const inside = light.x > minX + 0.6 && light.x < maxX - 0.6 && light.z > minZ + 0.6 && light.z < maxZ - 0.6;
  if (!inside) problems.push(`ceiling light at (${light.x}, ${light.z}) is outside the room`);
}

// --------------------------------------------------------------- drawing

const SCALE = 46;                                  // px per metre on the plan
const PAD_LEFT = 190;                              // room for the window/door labels outside the west wall
const PAD_RIGHT = 60;
const HEADER = 178;
const COLUMN = 430;                                // derived-numbers column on the right
const planW = room.width * SCALE;
const planH = room.depth * SCALE;
const elevH = room.height * SCALE;
const width = Math.ceil(PAD_LEFT + planW + COLUMN + PAD_RIGHT);
const height = Math.ceil(HEADER + planH + 92 + elevH + 92);

const canvas = createCanvas(width, height);
const ctx = canvas.getContext('2d');
ctx.fillStyle = '#17181A';
ctx.fillRect(0, 0, width, height);

// Plan coordinates: +z (blackboard) points up, +x points right. Not mirrored.
const px = (x) => PAD_LEFT + (x - minX) * SCALE;
const pz = (z) => HEADER + (maxZ - z) * SCALE;

const label = (text, x, y, size = 13, color = '#B9B3A6', align = 'left') => {
  ctx.fillStyle = color;
  ctx.font = `${size}px sans-serif`;
  ctx.textAlign = align;
  ctx.textBaseline = 'alphabetic';
  ctx.fillText(text, x, y);
};

// Greedy character wrapping: the column is fixed width, so long derived lines must break.
const wrap = (text, x, y, size, color, maxChars, lineHeight) => {
  let line = '';
  for (const word of text.split(' ')) {
    if ((line + ' ' + word).trim().length > maxChars) {
      label(line.trim(), x, y, size, color);
      y += lineHeight;
      line = word;
    } else {
      line += ' ' + word;
    }
  }
  label(line.trim(), x, y, size, color);
  return y + lineHeight;
};

const rect = (x, z, w, d, fill, stroke) => {
  const left = px(x - w / 2);
  const top = pz(z + d / 2);
  ctx.fillStyle = fill;
  ctx.fillRect(left, top, w * SCALE, d * SCALE);
  if (stroke) {
    ctx.strokeStyle = stroke;
    ctx.lineWidth = 1;
    ctx.strokeRect(left + 0.5, top + 0.5, w * SCALE - 1, d * SCALE - 1);
  }
};

// Title block.
label('POST-SCHOOL / PROTOTYPE CLASSROOM', PAD_LEFT, 46, 22, '#EDE7D6');
label(`${data.id}  ·  ${data.status}`, PAD_LEFT, 70, 13, '#8E8A84');
label(`room ${room.width} x ${room.depth} x ${room.height} m  ·  ${seatCount} seats  ·  ${data.windows.length} windows  ·  ${data.doors.length} doors`, PAD_LEFT, 92, 13, '#B9B3A6');
label('plan: +z up (blackboard), +x right · drawn from the same JSON the runtime builder reads', PAD_LEFT, 112, 12, '#6F6A61');
label('offline proof, not a Unity render', PAD_LEFT, 132, 12, '#6F6A61');

// Floor.
ctx.fillStyle = C.floor;
ctx.fillRect(px(minX), pz(maxZ), planW, planH);

// Painted aisle band.
rect(0, room.centerZ, 1.6, room.depth - 1.6, '#7d8896');

// Chalk circle on the floor.
if (data.chalkCircle) {
  ctx.strokeStyle = C.chalk;
  ctx.lineWidth = 2.5;
  ctx.beginPath();
  ctx.ellipse(px(data.chalkCircle.centerX), pz(data.chalkCircle.centerZ),
    data.chalkCircle.radius * SCALE, data.chalkCircle.radius * SCALE, 0, 0, Math.PI * 2);
  ctx.stroke();
}

// Walls, drawn as thick bands outside the floor edge, with gaps at every opening.
const wallBand = (wall) => {
  const t = room.wallThickness;
  if (wall === 'front') return { x: px(minX), y: pz(minZ), w: planW, h: t * SCALE, axis: 'x', origin: minX };
  if (wall === 'back') return { x: px(minX), y: pz(maxZ) - t * SCALE, w: planW, h: t * SCALE, axis: 'x', origin: minX };
  if (wall === 'west') return { x: px(minX) - t * SCALE, y: pz(maxZ), w: t * SCALE, h: planH, axis: 'z', origin: minZ };
  return { x: px(maxX), y: pz(maxZ), w: t * SCALE, h: planH, axis: 'z', origin: minZ };
};

for (const wall of ['front', 'back', 'west', 'east']) {
  const band = wallBand(wall);
  const wallOpenings = (byWall[wall] || []).slice().sort((a, b) => a.centerAlong - b.centerAlong);
  let cursor = 0;
  const segments = [];
  for (const opening of wallOpenings) {
    const from = opening.centerAlong - opening.width / 2;
    const to = opening.centerAlong + opening.width / 2;
    segments.push({ from: cursor, to: from, fill: C.wall });
    segments.push({ from, to, fill: opening.kind === 'door' ? '#3A3B3D' : C.glass });
    cursor = to;
  }
  segments.push({ from: cursor, to: wallLength(wall), fill: C.wall });

  for (const segment of segments) {
    if (segment.to - segment.from <= 0.001) continue;
    if (band.axis === 'x') {
      ctx.fillStyle = segment.fill;
      ctx.fillRect(band.x + segment.from * SCALE, band.y, (segment.to - segment.from) * SCALE, band.h);
    } else {
      // West/east bands are drawn top-down, so the along axis is inverted on screen.
      ctx.fillStyle = segment.fill;
      ctx.fillRect(band.x, band.y + (wallLength(wall) - segment.to) * SCALE, band.w, (segment.to - segment.from) * SCALE);
    }
  }
}

// Openings annotated on the plan.
for (const opening of openings) {
  const isDoor = opening.kind === 'door';
  if (opening.wall === 'front' || opening.wall === 'back') {
    const x = px(alongOrigin(opening.wall) + opening.centerAlong);
    const y = opening.wall === 'front' ? pz(minZ) : pz(maxZ);
    // Swinging leaf for doors.
    if (isDoor) {
      // The leaf always swings into the room, never over the header.
      ctx.strokeStyle = C.wood;
      ctx.lineWidth = 2;
      const radius = opening.width * SCALE;
      ctx.beginPath();
      if (opening.wall === 'front') ctx.arc(x, y, radius, -Math.PI / 2, 0);
      else ctx.arc(x, y, radius, 0, Math.PI / 2);
      ctx.stroke();
      ctx.beginPath();
      ctx.moveTo(x, y);
      ctx.lineTo(x + (opening.wall === 'front' ? radius : -radius), y);
      ctx.stroke();
    }
    label(opening.id, x, opening.wall === 'front' ? y - 12 : y + 16, 10, isDoor ? '#3a2f22' : '#2e3a3c', 'center');
  } else {
    const z = pz(minZ + opening.centerAlong);
    const x = opening.wall === 'west' ? px(minX) : px(maxX);
    if (isDoor) {
      ctx.strokeStyle = C.wood;
      ctx.lineWidth = 2;
      const radius = opening.width * SCALE;
      ctx.beginPath();
      if (opening.wall === 'west') ctx.arc(x, z, radius, -Math.PI / 2, 0);
      else ctx.arc(x, z, radius, Math.PI / 2, Math.PI);
      ctx.stroke();
    }
    label(opening.id, x + (opening.wall === 'west' ? -14 : 14), z, 10, isDoor ? C.wood : C.glass, opening.wall === 'west' ? 'right' : 'left');
  }
}

// Blackboard, podium, protagonist desk.
rect(0, maxZ - 0.1, data.blackboard.width, 0.18, C.board);
label('blackboard', px(0), pz(maxZ) + 26, 11, '#2A2A2A', 'center');
rect(data.podium.positionX, data.podium.positionZ, data.podium.width, data.podium.depth, C.wood, C.ink);
rect(data.podium.positionX, data.podium.positionZ - 0.9, seat.chairSize, seat.chairSize, C.wainscot);
label('podium', px(data.podium.positionX), pz(data.podium.positionZ) + 4, 11, '#2b2b2b', 'center');

// Seat grid, with the two impossibilities marked.
let index = 0;
const impossible = { 6: 'levitating', 17: 'sunk' };
for (const row of seat.rows) {
  for (const column of seat.columns) {
    rect(column, row, seat.deskWidth, seat.deskDepth, C.wood, '#6b573c');
    rect(column, row + seat.chairOffsetZ, seat.chairSize, seat.chairSize * 0.9, C.metal, '#2f3236');
    if (impossible[index]) {
      label(`${index} ${impossible[index]}`, px(column), pz(row + seat.chairOffsetZ) + 3, 9, C.figureAccent, 'center');
    }
    index++;
  }
}

// The protagonist's desk and drawn-out chair, inside the chalk circle.
rect(data.wakeDesk.positionX, data.wakeDesk.positionZ, data.wakeDesk.width, data.wakeDesk.depth, C.wood, C.ink);
rect(data.wakeDesk.positionX, data.wakeDesk.positionZ - 1.2, seat.chairSize, seat.chairSize, C.wainscot, C.ink);
// The standing protagonist: a small magenta-capsule-scale figure, the "small person, huge room" cue.
ctx.fillStyle = C.figureAccent;
ctx.beginPath();
ctx.arc(px(0), pz(1.45), 0.26 * SCALE, 0, Math.PI * 2);
ctx.fill();
label('protagonist wakes here', px(0) + 16, pz(1.45) + 24, 10, C.figureAccent);

// Ceiling lights, projected.
for (const [i, light] of data.ceilingLights.entries()) {
  ctx.fillStyle = light.tiltDegrees > 0 ? C.figureAccent : C.chalk;
  ctx.globalAlpha = 0.28;
  ctx.fillRect(px(light.x) - 0.07 * SCALE, pz(light.z) - (light.length / 2) * SCALE, 0.14 * SCALE, light.length * SCALE);
  ctx.globalAlpha = 1;
  label(`${i}${light.tiltDegrees > 0 ? ` tilt ${light.tiltDegrees}° @${light.height} m` : ''}`,
    px(light.x), pz(light.z) - (light.length / 2) * SCALE - 6, 9,
    light.tiltDegrees > 0 ? C.figureAccent : '#9a958a', 'center');
}

// Scale bar, kept clear of the exit door's swing arc.
ctx.fillStyle = '#B9B3A6';
ctx.fillRect(PAD_LEFT + planW - 2 * SCALE, HEADER + planH + 34, SCALE, 3);
label('1 m', PAD_LEFT + planW - SCALE + 8, HEADER + planH + 40, 11);

// Right-hand column: derived numbers.
const columnX = PAD_LEFT + planW + 46;
let y = HEADER + 4;
label('DERIVED FROM THE JSON', columnX, y, 12, '#8E8A84');
y += 22;
for (const line of degrees) {
  y = wrap(line, columnX, y, 11, '#B9B3A6', 56, 15) + 4;
}

y += 14;
label('PROBLEMS', columnX, y, 12, problems.length ? '#E0895F' : '#71806B');
y += 22;
if (!problems.length) {
  y = wrap('none: the layout satisfies the 24-seat classroom rules', columnX, y, 11, '#71806B', 56, 15);
}
for (const problem of problems) {
  y = wrap(problem, columnX, y, 11, '#E0895F', 56, 15) + 4;
}

// ---------------------------------------------------------------- elevation
// Front elevation looking towards the blackboard (+z): the wall the player faces on waking.
const elevTop = HEADER + planH + 92;
label('FRONT ELEVATION — looking towards the blackboard', PAD_LEFT, elevTop - 16, 13, '#EDE7D6');
const ex = (x) => PAD_LEFT + (x - minX) * SCALE;
const ey = (v) => elevTop + (room.height - v) * SCALE;

ctx.fillStyle = C.wall;
ctx.fillRect(ex(minX), ey(room.height), room.width * SCALE, room.height * SCALE);
ctx.fillStyle = C.wainscot;
ctx.fillRect(ex(minX), ey(0.9), room.width * SCALE, 0.9 * SCALE);

// Windows on the west wall are seen edge-on; draw the blackboard wall's features instead.
ctx.fillStyle = C.board;
ctx.fillRect(ex(-data.blackboard.width / 2), ey(data.blackboard.sillHeight + data.blackboard.height),
  data.blackboard.width * SCALE, data.blackboard.height * SCALE);
ctx.fillStyle = C.chalk;
ctx.fillRect(ex(-data.blackboard.width / 2), ey(data.blackboard.sillHeight) - 6, data.blackboard.width * SCALE, 4);

// Exit door in the front... in the blackboard wall there is a staff door: draw its outline.
for (const door of data.doors) {
  if (door.wall !== 'back') continue;
  const doorX = alongOrigin('back') + door.centerAlong;
  ctx.fillStyle = '#3A3B3D';
  ctx.fillRect(ex(doorX - door.width / 2), ey(door.height), door.width * SCALE, door.height * SCALE);
  label(door.id, ex(doorX), ey(door.height) - 6, 10, C.wood, 'center');
}

// Podium silhouette and the protagonist against the room's height.
ctx.fillStyle = C.wood;
ctx.fillRect(ex(data.podium.positionX - data.podium.width / 2), ey(data.podium.height),
  data.podium.width * SCALE, data.podium.height * SCALE);

const figureX = 2.6;
ctx.fillStyle = C.coat;
ctx.fillRect(ex(figureX) - 0.2 * SCALE, ey(1.46), 0.4 * SCALE, 0.53 * SCALE);   // torso + coat
ctx.fillStyle = C.trousers;
ctx.fillRect(ex(figureX) - 0.13 * SCALE, ey(0.88), 0.26 * SCALE, 0.88 * SCALE);  // legs
ctx.fillStyle = C.skin;
ctx.fillRect(ex(figureX) - 0.1 * SCALE, ey(1.71), 0.2 * SCALE, 0.25 * SCALE);    // head
ctx.fillStyle = C.hair;
ctx.fillRect(ex(figureX) - 0.11 * SCALE, ey(1.755), 0.22 * SCALE, 0.07 * SCALE);
ctx.fillStyle = C.figureAccent;
ctx.fillRect(ex(figureX) + 0.15 * SCALE, ey(1.19), 0.1 * SCALE, 0.045 * SCALE);   // sleeve band
label('protagonist 1.72 m · 7 heads · ageless', ex(figureX) + 0.42 * SCALE, ey(1.62), 11, '#8a5a3a');

// Height ticks.
for (let h = 0; h <= room.height; h += 0.5) {
  ctx.fillStyle = '#3a3b3d';
  ctx.fillRect(ex(minX) - 8, ey(h), 8, 1);
  label(`${h.toFixed(1)}`, ex(minX) - 12, ey(h) + 4, 10, '#8E8A84', 'right');
}
label('floor', ex(minX) + 6, ey(0) - 6, 10, '#8E8A84');

// Ceiling-light heights, including the tilted one.
for (const light of data.ceilingLights) {
  ctx.fillStyle = light.tiltDegrees > 0 ? C.figureAccent : C.chalk;
  ctx.fillRect(ex(light.x) - 0.1 * SCALE, ey(light.height) - 3, 0.2 * SCALE, 6);
}

const summary = problems.length
  ? `${problems.length} PROBLEM(S) — see the report`
  : 'PLAN PROOF OK — 24 seats, 2 windows, 2 doors, blackboard, podium';
label(summary, PAD_LEFT, height - 24, 13, problems.length ? '#E0895F' : '#71806B');

const outputName = path.join(outDir, 'classroom-plan-proof.png');
fs.mkdirSync(outDir, { recursive: true });
fs.writeFileSync(outputName, canvas.toBuffer('image/png'));

console.log(`wrote ${outputName} (${width}x${height})`);
console.log(`derived: ${seatCount} seats, ${data.windows.length} windows, ${data.doors.length} doors, ${data.ceilingLights.length} ceiling lights`);
for (const line of degrees) console.log(`  · ${line}`);
if (problems.length) {
  console.error(`\n${problems.length} problem(s):`);
  for (const problem of problems) console.error(`  - ${problem}`);
  process.exitCode = 1;
} else {
  console.log('\nno problems: the layout satisfies the classroom rules.');
}
