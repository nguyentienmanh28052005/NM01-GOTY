// Tham chiếu AgX của three.js r160 (tonemapping_pars_fragment.glsl.js), chép nguyên công thức để tạo số liệu test cho bản C#/HLSL.
// mat3 GLSL nhận CỘT; M*v = c0*v.x + c1*v.y + c2*v.z
const mul = (cols, v) => [0, 1, 2].map(j => cols[0][j] * v[0] + cols[1][j] * v[1] + cols[2][j] * v[2]);
const REC2020_TO_SRGB = [[1.6605, -0.1246, -0.0182], [-0.5876, 1.1329, -0.1006], [-0.0728, -0.0083, 1.1187]];
const SRGB_TO_REC2020 = [[0.6274, 0.0691, 0.0164], [0.3293, 0.9195, 0.0880], [0.0433, 0.0113, 0.8956]];
const INSET = [[0.856627153315983, 0.137318972929847, 0.11189821299995], [0.0951212405381588, 0.761241990602591, 0.0767994186031903], [0.0482516061458583, 0.101439036467562, 0.811302368396859]];
const OUTSET = [[1.1271005818144368, -0.1413297634984383, -0.14132976349843826], [-0.11060664309660323, 1.157823702216272, -0.11060664309660294], [-0.016493938717834573, -0.016493938717834257, 1.2519364065950405]];
const MIN_EV = -12.47393, MAX_EV = 4.026069;
const contrast = x => { const x2 = x * x, x4 = x2 * x2; return 15.5 * x4 * x2 - 40.14 * x4 * x + 31.96 * x4 - 6.868 * x2 * x + 0.4298 * x2 + 0.1191 * x - 0.00232; };
function agx(c, exposure = 1.02) {
  c = c.map(v => v * exposure);
  c = mul(SRGB_TO_REC2020, c); c = mul(INSET, c);
  c = c.map(v => Math.min(1, Math.max(0, (Math.log2(Math.max(v, 1e-10)) - MIN_EV) / (MAX_EV - MIN_EV))));
  c = c.map(contrast); c = mul(OUTSET, c);
  c = c.map(v => Math.pow(Math.max(0, v), 2.2)); c = mul(REC2020_TO_SRGB, c);
  return c.map(v => Math.min(1, Math.max(0, v)));
}
const samples = [[0, 0, 0], [0.18, 0.18, 0.18], [1, 1, 1], [4, 2, 1], [0.05, 0.1, 0.3], [16, 16, 16]];
for (const s of samples) console.log(JSON.stringify(s), JSON.stringify(agx(s).map(v => +v.toFixed(6))));
