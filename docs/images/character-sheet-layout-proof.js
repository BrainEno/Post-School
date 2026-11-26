// Offline layout proof for the character sheet. NOT a Unity render.
//
// It mirrors the normalised rects in Assets/Scripts/UI/CharacterSheetView.cs — including the nested
// frames (overlay > sheet > ticket / card) and Unity's bottom-left anchor convention — emulates
// CanvasScaler (ScaleWithScreenSize 1600x900, match 0.5) and legacy UGUI greedy wrapping, then
// reports any text that overflows its box or any line that is too wide.
//
// Usage: node character-sheet-layout-proof.js [text.json] [outDir]
// Dependencies (not committed): npm i @napi-rs/canvas @expo-google-fonts/noto-sans-sc @expo-google-fonts/noto-serif-sc
// Run from the directory that has node_modules:
//   node docs/images/character-sheet-layout-proof.js \
//     Assets/Resources/CharacterSheet/character_sheet_text_placeholder.json /tmp/out
const fs = require('fs');
const { createCanvas, GlobalFonts } = require('@napi-rs/canvas');

const fontRoot = require.resolve('@expo-google-fonts/noto-sans-sc/package.json').replace('/package.json', '');
const serifRoot = require.resolve('@expo-google-fonts/noto-serif-sc/package.json').replace('/package.json', '');
GlobalFonts.registerFromPath(`${fontRoot}/400Regular/NotoSansSC_400Regular.ttf`, 'SansSC');
GlobalFonts.registerFromPath(`${fontRoot}/700Bold/NotoSansSC_700Bold.ttf`, 'SansSCBold');
GlobalFonts.registerFromPath(`${serifRoot}/400Regular/NotoSerifSC_400Regular.ttf`, 'SerifSC');

const data = JSON.parse(fs.readFileSync(
  process.argv[2] || 'Assets/Resources/CharacterSheet/character_sheet_text_placeholder.json', 'utf8'));
const outDir = process.argv[3] || '.';

const C = {
  backdrop: '#14150F', paper: '#EDE7D6', paperShade: '#DBD2B9', paperLift: '#F6F1E2',
  ink: '#1A1A18', ochre: '#D99A2B', oxblood: '#7A2E2A', tape: '#E9DFBF',
  pencil: ['#7A2E2A', '#3F4A6B', '#1A1A18', '#5F6B4A', '#D99A2B'],
};
const ORDER = ['Physique', 'Psyche', 'Intellect', 'Perception', 'Imagination'];
const NAMES = { Physique: '体魄', Psyche: '精神', Intellect: '智性', Perception: '感知', Imagination: '想象' };

// Proof state: two points spent (体魄 2, 想象 2), 体魄 selected.
const build = { Physique: 2, Psyche: 1, Intellect: 1, Perception: 1, Imagination: 2 };
const selected = 0;
const remaining = 7 - Object.values(build).reduce((sum, v) => sum + v - 1, 0);
const total = Object.values(build).reduce((sum, v) => sum + v, 0);

const spaceOut = (value) => {
  let out = '';
  for (let i = 0; i < value.length; i++) {
    out += value[i];
    const c = value.charCodeAt(i);
    const next = value.charCodeAt(i + 1);
    if (c >= 0x2e80 && c <= 0x9fff && next >= 0x2e80) out += '\u2009';
  }
  return out;
};
const levelWord = (v) => ({ 1: '基础', 2: '低', 3: '中', 4: '高', 5: '极高' }[v] || '超出刻度');
const budgetSentence = (rem) =>
  rem > 0 ? `还有 ${rem} 点未被写下。点选任意一项，把它分配出去。` : '七点已全部分配。可以开始。';

function drawScene(width, height, outPath) {
  const canvas = createCanvas(width, height);
  const ctx = canvas.getContext('2d');
  const problems = [];
  const uiScale = Math.sqrt((width / 1600) * (height / 900));

  // A Frame is a normalised rect in screen space (Unity bottom-left origin).
  const frame = (x0, y0, x1, y1, parent) => {
    const p = parent || { x: 0, y: 0, w: 1, h: 1 };
    return { x: p.x + x0 * p.w, y: p.y + y0 * p.h, w: (x1 - x0) * p.w, h: (y1 - y0) * p.h };
  };
  // Sub-rect inside a frame, in that frame's own normalised coordinates.
  const sub = (f, x0, y0, x1, y1) => frame(x0, y0, x1, y1, f);
  const px = (r) => ({ x: r.x * width, y: (1 - r.y - r.h) * height, w: r.w * width, h: r.h * height });

  const fill = (r, color) => {
    const p = px(r);
    ctx.fillStyle = color;
    ctx.fillRect(p.x, p.y, p.w, p.h);
  };
  const rotateRect = (r, degrees, color) => {
    const p = px(r);
    ctx.save();
    ctx.translate(p.x + p.w / 2, p.y + p.h / 2);
    ctx.rotate((degrees * Math.PI) / 180);
    ctx.fillStyle = color;
    ctx.fillRect(-p.w / 2, -p.h / 2, p.w, p.h);
    ctx.restore();
  };

  function wrapPixels(value, maxWidth) {
    const lines = [];
    let line = '';
    for (const ch of value) {
      const candidate = line + ch;
      if (ctx.measureText(candidate).width > maxWidth && line !== '') {
        lines.push(line);
        line = ch;
      } else {
        line = candidate;
      }
    }
    lines.push(line);
    return lines;
  }

  function text(r, value, size, color, opts = {}) {
    const { align = 'left', valign = 'middle', serif = false, bold = false, lineHeight = 1.25, name = 'text', maxLines = null } = opts;
    const p = px(r);
    const fontSize = Math.max(1, size * uiScale);
    ctx.font = `${bold ? 'bold ' : ''}${fontSize}px "${serif ? 'SerifSC' : 'SansSC'}"`;
    ctx.fillStyle = color;
    const lines = [];
    for (const paragraph of value.split('\n')) {
      lines.push(...wrapPixels(paragraph, Math.max(1, p.w - 4 * uiScale)));
    }
    const lh = fontSize * lineHeight;
    ctx.textAlign = align === 'center' ? 'center' : align === 'right' ? 'right' : 'left';
    const x = align === 'center' ? p.x + p.w / 2 : align === 'right' ? p.x + p.w - 2 : p.x + 2;
    const blockTop = valign === 'middle' ? p.y + p.h / 2 - (lines.length * lh) / 2 : p.y;
    if (lines.length * lh > p.h + 1) {
      problems.push(`${name}: ${lines.length} lines at ${Math.round(lh)}px exceed box height ${Math.round(p.h)}px`);
    }
    if (maxLines && lines.length > maxLines) problems.push(`${name}: ${lines.length} lines > ${maxLines}`);
    lines.forEach((line, index) => {
      ctx.fillText(line, x, blockTop + lh * (index + 0.5) + lh * 0.06);
    });
    if (ctx.measureText(lines[lines.length - 1] || '').width > p.w + 1) {
      problems.push(`${name}: last line wider than box (${Math.round(p.w)}px)`);
    }
  }

  function wobbleEllipse(cx, cy, rx, ry, stroke, color, seed) {
    ctx.strokeStyle = color;
    ctx.lineWidth = stroke * uiScale;
    ctx.beginPath();
    for (let i = 0; i <= 72; i++) {
      const a = (i / 72) * Math.PI * 2;
      const w = 1 + Math.sin(a * 3 + seed) * 0.012 + Math.sin(a * 7 + seed * 0.5) * 0.008;
      const x = cx + Math.cos(a) * rx * w;
      const y = cy + Math.sin(a) * ry * w;
      if (i === 0) ctx.moveTo(x, y);
      else ctx.lineTo(x, y);
    }
    ctx.stroke();
  }

  const ruleLine = (f, x0, y, x1, color, weight) => {
    const a = px(frame(x0, y, x1, y, f));
    ctx.strokeStyle = color;
    ctx.lineWidth = weight * uiScale;
    ctx.beginPath();
    ctx.moveTo(a.x, a.y);
    ctx.lineTo(a.x + a.w, a.y + 1);
    ctx.stroke();
  };

  // ---- backdrop -------------------------------------------------------------
  fill(frame(0, 0, 1, 1), C.backdrop);
  rotateRect(frame(0.03, 0.62, 0.16, 0.72), 4.5, 'rgba(219,210,185,0.10)');
  rotateRect(frame(0.86, 0.08, 0.97, 0.14), -6, 'rgba(122,46,42,0.16)');

  const underSheet = frame(0.068, 0.062, 0.938, 0.962);
  rotateRect(underSheet, 1.6, 'rgba(219,210,185,0.55)');
  const sheetFrame = frame(0.055, 0.045, 0.945, 0.955);
  rotateRect(sheetFrame, -1.0, C.paper);

  // tape
  rotateRect(sub(sheetFrame, 0.30, 0.985, 0.44, 1.032), -2.4, 'rgba(233,223,191,0.72)');
  rotateRect(sub(sheetFrame, 0.958, 0.55, 0.988, 0.66), 5, 'rgba(233,223,191,0.72)');

  // index tabs
  for (let i = 0; i < 5; i++) {
    const top = 0.79 - i * 0.052;
    const tab = sub(sheetFrame, 0.985, top - 0.044, 1.024, top);
    fill(tab, 'rgba(219,210,185,0.9)');
    const t = px(tab);
    ctx.fillStyle = C.pencil[i];
    ctx.fillRect(t.x + 1, t.y + 4 * uiScale, 3 * uiScale, t.h - 8 * uiScale);
  }

  const S = sheetFrame;

  // ---- header ---------------------------------------------------------------
  text(sub(S, 0.050, 0.895, 0.52, 0.972), data.header, 42, C.ink, { serif: true, name: 'header' });
  text(sub(S, 0.052, 0.855, 0.60, 0.897), spaceOut(data.subheader), 11, 'rgba(26,26,24,0.6)', { name: 'subheader' });
  text(sub(S, 0.615, 0.908, 0.945, 0.946), spaceOut('读数区 · 未分配'), 11, 'rgba(26,26,24,0.45)', { align: 'right', name: 'columnLabel' });
  ruleLine(S, 0.048, 0.845, 0.952, 'rgba(26,26,24,0.5)', 2.4);
  text(sub(S, 0.050, 0.775, 0.595, 0.838), spaceOut(data.instruction), 16, 'rgba(26,26,24,0.8)', { serif: true, name: 'instruction', lineHeight: 1.2 });

  // ---- quota ticket ---------------------------------------------------------
  const ticketShadow = sub(S, 0.622, 0.788, 0.952, 0.900);
  rotateRect(ticketShadow, 2.2, 'rgba(26,26,24,0.14)');
  const ticket = sub(S, 0.618, 0.794, 0.948, 0.904);
  rotateRect(ticket, 1.4, C.paperLift);
  rotateRect(sub(ticket, 0.36, 0.76, 0.62, 1.24), -3.5, 'rgba(233,223,191,0.72)');
  text(sub(ticket, 0.06, 0.70, 0.55, 0.94), spaceOut(data.budgetLabel), 11, 'rgba(26,26,24,0.7)', { name: 'quotaLabel' });
  text(sub(ticket, 0.05, 0.10, 0.40, 0.70), String(remaining), 40, C.ink, { serif: true, bold: true, name: 'quotaValue' });
  text(sub(ticket, 0.40, 0.16, 0.94, 0.50), spaceOut(`/ 7　基础 1 · 上限 5`), 12, 'rgba(26,26,24,0.6)', { serif: true, name: 'quotaScale' });

  // ---- attribute rows -------------------------------------------------------
  const rowHeight = 0.0765;
  const rowGap = 0.0075;
  for (let i = 0; i < 5; i++) {
    const key = ORDER[i];
    const entry = data.attributes.find((a) => a.attribute === key);
    const value = build[key];
    const top = 0.762 - i * (rowHeight + rowGap);
    const rowFrame = sub(S, 0.050, top - rowHeight, 0.600, top);

    fill(rowFrame, i % 2 === 0 ? 'rgba(219,210,185,0.32)' : 'rgba(246,241,226,0.30)');
    const bar = sub(S, 0.0535, top - rowHeight + 0.008, 0.0595, top - 0.008);
    const barPx = px(bar);
    ctx.globalAlpha = selected === i ? 0.95 : 0.5;
    ctx.fillStyle = C.pencil[i];
    ctx.fillRect(barPx.x, barPx.y, barPx.w, barPx.h);
    ctx.globalAlpha = 1;

    text(sub(S, 0.066, top - rowHeight, 0.102, top), spaceOut(String(i + 1).padStart(2, '0')), 12, 'rgba(26,26,24,0.42)', { name: `index${i}` });
    text(sub(S, 0.106, top - rowHeight, 0.200, top), NAMES[key], 28, selected === i ? C.ink : 'rgba(26,26,24,0.84)', { serif: true, name: `name${i}` });
    text(sub(S, 0.202, top - rowHeight + 0.003, 0.246, top - 0.003), String(value), 38, C.ink, { serif: true, align: 'center', name: `value${i}` });
    text(sub(S, 0.248, top - rowHeight + 0.020, 0.302, top - 0.020), spaceOut(levelWord(value)), 10, 'rgba(26,26,24,0.55)', { name: `level${i}` });

    for (let slot = 0; slot < 5; slot++) {
      const left = 0.312 + slot * 0.0405;
      const ringFrame = sub(S, left, top - rowHeight + 0.026, left + 0.0265, top - 0.008);
      const p = px(ringFrame);
      const filled = slot < value;
      const preview = remaining > 0 && slot === value;
      const color = filled
        ? C.pencil[i]
        : preview
          ? (selected === i ? 'rgba(217,154,43,0.55)' : 'rgba(26,26,24,0.22)')
          : 'rgba(26,26,24,0.16)';
      wobbleEllipse(p.x + p.w / 2, p.y + p.h / 2, p.w * 0.46, p.h * 0.44, filled ? 2.8 : preview ? 2 : 1.4, color, i * 17 + slot * 5 + 1);
      text(sub(S, left - 0.004, top - rowHeight, left + 0.0305, top - rowHeight + 0.026), entry.rungs[slot], 10,
        filled ? 'rgba(26,26,24,0.8)' : 'rgba(26,26,24,0.38)', { align: 'center', name: `rung${i}-${slot}` });
    }

    const button = sub(S, 0.520, top - rowHeight + 0.018, 0.560, top - 0.018);
    const b = px(button);
    ctx.globalAlpha = 0.85;
    ctx.fillStyle = C.pencil[i];
    ctx.fillRect(b.x, b.y, b.w, b.h);
    ctx.globalAlpha = 1;
    text(button, '＋', 22, 'rgba(246,241,226,0.95)', { align: 'center', name: `plus${i}` });

    ruleLine(S, 0.060, top - rowHeight + 0.003, 0.592, 'rgba(26,26,24,0.22)', 1.2);
  }

  // ---- signature block ------------------------------------------------------
  const thumb = sub(S, 0.062, 0.128, 0.140, 0.235);
  const tp = px(thumb);
  for (let ring = 0; ring < 6; ring++) {
    const factor = 1 - ring * 0.15;
    wobbleEllipse(tp.x + tp.w / 2, tp.y + tp.h / 2, (tp.w * 0.42) * factor, (tp.h * 0.40) * factor, 1.6,
      `rgba(122,46,42,${0.75 - ring * 0.09})`, 4 + ring * 13);
  }
  ruleLine(S, 0.165, 0.135, 0.560, 'rgba(26,26,24,0.55)', 1.6);
  text(sub(S, 0.165, 0.100, 0.560, 0.133), spaceOut('签名 · 本人 · 未到校'), 11, 'rgba(26,26,24,0.45)', { name: 'signatureLabel' });
  text(sub(S, 0.050, 0.048, 0.560, 0.095), spaceOut(data.draftNotice), 11, 'rgba(26,26,24,0.45)', { name: 'draftNotice', lineHeight: 1.3 });

  const quartet = sub(S, 0.500, 0.150, 0.578, 0.290);
  const qp = px(quartet);
  for (let q = 0; q < 4; q++) {
    ctx.strokeStyle = 'rgba(217,154,43,0.55)';
    ctx.lineWidth = 1.8 * uiScale;
    ctx.beginPath();
    ctx.arc(qp.x + qp.w / 2, qp.y + qp.h / 2, Math.min(qp.w, qp.h) * 0.46,
      ((q * 90 + 9) * Math.PI) / 180, (((q + 1) * 90 - 9) * Math.PI) / 180);
    ctx.stroke();
  }

  // ---- description card -----------------------------------------------------
  rotateRect(sub(S, 0.622, 0.318, 0.952, 0.786), -1.2, 'rgba(26,26,24,0.12)');
  const card = sub(S, 0.618, 0.322, 0.948, 0.790);
  rotateRect(card, -0.7, C.paperLift);

  const cp = px(card);
  ctx.save();
  ctx.beginPath();
  ctx.rect(cp.x, cp.y, cp.w, cp.h);
  ctx.clip();
  ctx.strokeStyle = 'rgba(26,26,24,0.11)';
  ctx.lineWidth = 1;
  for (let y = cp.y + 15 * uiScale; y < cp.y + cp.h; y += 15 * uiScale) {
    ctx.beginPath();
    ctx.moveTo(cp.x, y);
    ctx.lineTo(cp.x + cp.w, y);
    ctx.stroke();
  }
  ctx.strokeStyle = 'rgba(122,46,42,0.5)';
  ctx.lineWidth = 1.5 * uiScale;
  ctx.beginPath();
  ctx.moveTo(cp.x + 34 * uiScale, cp.y);
  ctx.lineTo(cp.x + 34 * uiScale, cp.y + cp.h);
  ctx.stroke();
  ctx.restore();

  rotateRect(sub(card, 0.05, 0.93, 0.19, 1.04), 7, 'rgba(233,223,191,0.72)');

  const selKey = ORDER[selected];
  const selEntry = data.attributes.find((a) => a.attribute === selKey);
  const selValue = build[selKey];

  text(sub(card, 0.090, 0.845, 0.55, 0.955), NAMES[selKey], 26, 'rgba(122,46,42,0.95)', { serif: true, name: 'cardTitle' });
  text(sub(card, 0.090, 0.755, 0.95, 0.845), spaceOut('「' + selEntry.voice + '」'), 15, 'rgba(26,26,24,0.6)', { serif: true, name: 'cardVoice' });
  text(sub(card, 0.090, 0.40, 0.95, 0.74), spaceOut(selEntry.narrative), 17, C.ink, { serif: true, lineHeight: 1.3, name: 'cardNarrative', maxLines: 5 });
  text(sub(card, 0.090, 0.275, 0.95, 0.39), spaceOut(data.skillLabel + ' · ' + selEntry.skills), 12, 'rgba(26,26,24,0.62)', { name: 'cardSkills', maxLines: 3 });

  const threshold = selValue >= 5
    ? `${data.thresholdLabel} · 已达暂定上限 5`
    : `${data.thresholdLabel} · ${NAMES[selKey]} ${selValue} / ${selValue + 1} · 未达到`;
  text(sub(card, 0.090, 0.035, 0.95, 0.255), spaceOut(threshold + '\n低 · ' + selEntry.lowNote + '　高 · ' + selEntry.highNote), 11,
    'rgba(26,26,24,0.5)', { name: 'cardNotes', maxLines: 5, lineHeight: 1.25 });

  // ---- footer ---------------------------------------------------------------
  text(sub(S, 0.618, 0.268, 0.948, 0.310), spaceOut(budgetSentence(remaining) + `　总 ${total} / 12`), 11,
    'rgba(26,26,24,0.55)', { name: 'budgetDetail', maxLines: 2 });

  const reset = sub(S, 0.618, 0.196, 0.772, 0.258);
  fill(reset, C.paperShade);
  text(reset, spaceOut(data.resetLabel), 13, C.ink, { align: 'center', name: 'resetButton' });

  const confirm = sub(S, 0.782, 0.196, 0.948, 0.258);
  fill(confirm, remaining === 0 ? C.ochre : 'rgba(217,154,43,0.45)');
  text(confirm, spaceOut(data.confirmLabel), 16, C.paper, { align: 'center', name: 'confirmButton' });

  text(sub(S, 0.618, 0.148, 0.948, 0.188), spaceOut(data.catalogueLabel), 11, 'rgba(26,26,24,0.42)', { name: 'catalogue' });
  text(sub(S, 0.618, 0.100, 0.948, 0.150), spaceOut(data.catalogueNote), 10, 'rgba(26,26,24,0.4)', { name: 'catalogueNote', lineHeight: 1.2, maxLines: 3 });

  fs.writeFileSync(outPath, canvas.toBuffer('image/png'));
  console.log(`${outPath.split('/').pop()}: uiScale ${uiScale.toFixed(2)}, ${problems.length} layout problem(s)`);
  problems.forEach((p) => console.log('  - ' + p));
  return problems.length;
}

let failures = 0;
failures += drawScene(1600, 900, `${outDir}/character-sheet-proof-1600x900.png`);
failures += drawScene(1024, 768, `${outDir}/character-sheet-proof-1024x768.png`);
process.exit(failures > 0 ? 1 : 0);
