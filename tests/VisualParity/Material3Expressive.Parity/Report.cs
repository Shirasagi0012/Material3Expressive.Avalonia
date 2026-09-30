using System.Net;
using System.Text.Json;

namespace Material3Expressive.Parity;

public static class Report
{
    public static void Write(string directory, Scenario s, Comparison comparison, Capture? expected, Capture? actual)
    {
        var availableDiffFrames = s.Checkpoints.Where(i => File.Exists(Path.Combine(directory, "diff", $"{i:D4}.png"))).ToArray();
        var data = JsonSerializer.Serialize(new { scenario = s, comparison, expected, actual, availableDiffFrames }, ParityJson.Options);
        var html = """
<!doctype html><html lang="zh"><meta charset="utf-8"><title>Material3 visual parity</title>
<style>body{font:15px system-ui;margin:24px;color:#222;background:#f7f7fb}h1{font-size:22px}.images{display:flex;gap:20px;flex-wrap:wrap}img{image-rendering:pixelated;border:1px solid #ccc;max-width:100%}article{background:white;padding:16px;margin:16px 0;border-radius:8px}table{border-collapse:collapse;width:100%}td,th{padding:6px;text-align:left;border-bottom:1px solid #ddd}pre{white-space:pre-wrap;overflow-wrap:anywhere}input{width:350px}canvas{background:white;border:1px solid #ccc;max-width:100%}.FAIL,.ERROR{color:#b00020}.PASS{color:#166534}.UNVERIFIED{color:#905000}</style>
<h1 id="title"></h1><p id="replay"></p><p><a href="comparison.json">comparison.json</a> · <a href="scenario.json">scenario.json</a> · <a href="profile.json">profile.json</a></p>
<article><label>检查点 <select id="checkpoint"></select></label> <label>图层 <select id="layer"><option value="full">完整图像</option><option value="without">保留布局，隐藏文字绘制</option><option value="paint">文字绘制贡献（诊断）</option></select></label> <label>叠加透明度 <input id="opacity" type="range" min="0" max="1" step="0.01" value="0.5"></label>
<div class="images"><div>Compose<br><img id="expected"></div><div>Avalonia<br><img id="actual"></div><div>差异<br><img id="diff"></div><div>叠加<br><canvas id="overlay"></canvas></div></div></article>
<article><label>轨迹 <select id="metric"></select></label><canvas id="trace" width="800" height="180"></canvas><p>蓝色：Compose；橙色：Avalonia；灰色竖线：输入事件。轨迹使用原始场景时间。</p></article>
<article><label>差异 <select id="filter"><option value="all">全部</option><option value="frame">当前检查点</option></select></label><table><thead><tr><th>状态</th><th>帧</th><th>区域</th><th>指标</th><th>参考</th><th>实际</th><th>差值／说明</th></tr></thead><tbody id="findings"></tbody></table></article>
<article><details><summary>环境、能力与原始观测</summary><pre id="observations"></pre></details></article>
<script>
const d=__DATA__;
const $=id=>document.getElementById(id),esc=x=>String(x??'').replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
$('title').textContent=`${d.comparison.status} · ${d.comparison.kind} · ${d.scenario.id}`;
$('replay').textContent=`复现：.\\tests\\VisualParity\\parity.ps1 run --case ${d.scenario.id}`;
$('observations').textContent=JSON.stringify({expected:d.expected,actual:d.actual},null,2);
for(const i of d.scenario.checkpoints){const o=document.createElement('option');o.value=i;o.textContent=`frame ${i} · ${d.scenario.framesUs[i]/1000} ms`;$('checkpoint').append(o)}
function findings(){const f=+$('checkpoint').value;$('findings').innerHTML=d.comparison.findings.filter(x=>$('filter').value==='all'||x.frame===f||x.frame<0).map(x=>`<tr><td class="${esc(x.status)}">${esc(x.status)}</td><td>${x.frame}</td><td>${esc(x.subject)}</td><td>${esc(x.metric)}</td><td>${esc(x.expected)}</td><td>${esc(x.actual)}</td><td>${esc(x.delta)} ${esc(x.detail)}</td></tr>`).join('')}
function overlay(){const a=$('expected'),b=$('actual'),c=$('overlay');if(!a.complete||!b.complete||!a.naturalWidth||!b.naturalWidth)return;c.width=a.naturalWidth;c.height=a.naturalHeight;const x=c.getContext('2d');x.drawImage(a,0,0);x.globalAlpha=+$('opacity').value;x.drawImage(b,0,0)}
function checkpoint(){const i=+$('checkpoint').value,name=`${String(i).padStart(4,'0')}.png`,layer=$('layer').value,e=d.expected?.frames[i],a=d.actual?.frames[i];const paths=layer==='full'?[e?.image&&`expected/${e.image}`,a?.image&&`actual/${a.image}`,d.availableDiffFrames.includes(i)&&`diff/${name}`]:layer==='without'?[e?.withoutTextImage&&`expected/${e.withoutTextImage}`,a?.withoutTextImage&&`actual/${a.withoutTextImage}`,d.availableDiffFrames.includes(i)&&e?.withoutTextImage&&a?.withoutTextImage&&`diff-without-text/${name}`]:[d.availableDiffFrames.includes(i)&&e?.withoutTextImage&&`text-paint-expected/${name}`,d.availableDiffFrames.includes(i)&&a?.withoutTextImage&&`text-paint-actual/${name}`,d.availableDiffFrames.includes(i)&&e?.withoutTextImage&&a?.withoutTextImage&&`diff-text-paint/${name}`];for(const [n,p]of [['expected',paths[0]],['actual',paths[1]],['diff',paths[2]]]){const img=$(n);img.parentElement.hidden=!p;if(p)img.src=p;else img.removeAttribute('src')}$('overlay').parentElement.hidden=!paths[0]||!paths[1];findings();plot()}
const keys=[...new Set([...(d.expected?.frames??[]),...(d.actual?.frames??[])].flatMap(x=>Object.keys(x.values)))];for(const key of keys){const o=document.createElement('option');o.value=key;o.textContent=key;$('metric').append(o)}
function plot(){const c=$('trace'),x=c.getContext('2d');x.clearRect(0,0,c.width,c.height);const k=$('metric').value,e=d.expected?.frames??[],a=d.actual?.frames??[],vs=[...e,...a].map(f=>f.values[k]).filter(Number.isFinite);if(!vs.length){x.fillText('此场景未导出动画数值；检查逐帧图像与布局。',12,24);return}const lo=Math.min(...vs),hi=Math.max(...vs),t=d.scenario.framesUs.at(-1)||1,px=n=>30+n/t*740,py=v=>150-(v-lo)/(hi-lo||1)*125;for(const ev of d.scenario.events){x.strokeStyle='#bbb';x.beginPath();x.moveTo(px(ev.atUs),10);x.lineTo(px(ev.atUs),155);x.stroke()}for(const [fs,col]of[[e,'#2563eb'],[a,'#ea580c']]){x.strokeStyle=col;x.beginPath();let started=false;for(const f of fs){if(!Number.isFinite(f.values[k]))continue;const p=[px(f.timeUs),py(f.values[k])];started?x.lineTo(...p):x.moveTo(...p);started=true}x.stroke()}x.fillStyle='#222';x.fillText(`${k}: ${lo.toFixed(4)} … ${hi.toFixed(4)}`,30,175)}
$('expected').onload=overlay;$('actual').onload=overlay;$('opacity').oninput=overlay;$('checkpoint').onchange=checkpoint;$('layer').onchange=checkpoint;$('filter').onchange=findings;$('metric').onchange=plot;checkpoint();
</script></html>
""";
        File.WriteAllText(Path.Combine(directory, "report.html"), html.Replace("__DATA__", data));
    }
    public static void Suite(string directory, IReadOnlyList<Comparison> results)
    {
        var rows = string.Join("\n", results.Select(x => $"<li><a href='{WebUtility.HtmlEncode(x.Scenario)}/report.html'>{WebUtility.HtmlEncode(x.Scenario)}</a> — {x.Status} ({x.Findings.Length}, {x.Kind})</li>"));
        File.WriteAllText(Path.Combine(directory, "report.html"), $"<!doctype html><meta charset='utf-8'><title>Parity suite</title><h1>Material3 visual parity</h1><p>parity 为双端一致性；repeatability 为重复执行和截图密度自检，不证明双端一致性。未覆盖能力见各场景报告。</p><ul>{rows}</ul><a href='summary.json'>summary.json</a>");
    }
}
