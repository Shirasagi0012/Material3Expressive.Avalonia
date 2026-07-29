#!/usr/bin/env python3
"""Visual parity report previewer — dynamically picks latest run.

Usage: python tools/visual-parity/preview.py [--port 8765]
"""

import json
import sys
import webbrowser
from pathlib import Path
from flask import Flask, jsonify, send_file, send_from_directory

ROOT = Path(__file__).resolve().parent.parent.parent
ARTIFACTS = ROOT / "artifacts" / "visual-parity" / "xunit"
VISUAL_PARITY_DIR = ARTIFACTS.parent  # artifacts/visual-parity/
PROFILES_PATH = ROOT / "tests" / "VisualParity" / "comparison-profiles.json"
PORT = 8765

app = Flask(__name__)

INDEX_HTML = r"""<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>Visual Parity Report</title>
<style>
:root { color-scheme: dark; }
* { box-sizing: border-box; margin: 0; padding: 0; }
body { font: 13px/1.5 system-ui, sans-serif; background: #0d1117; color: #c9d1d9; display: flex; height: 100vh; overflow: hidden; }
#sidebar { width: 320px; min-width: 260px; background: #161b22; border-right: 1px solid #30363d; display: flex; flex-direction: column; }
#run-selector { padding: 8px 12px; border-bottom: 1px solid #30363d; }
#run-selector select { width: 100%; padding: 5px 8px; background: #0d1117; color: #c9d1d9; border: 1px solid #30363d; border-radius: 4px; font-size: 12px; }
#stats { padding: 12px 16px; border-bottom: 1px solid #30363d; display: flex; gap: 8px; flex-wrap: wrap; }
.stat { padding: 4px 10px; border-radius: 12px; font-size: 12px; font-weight: 600; }
.stat-pass { background: #1a3a2a; color: #3fb950; }
.stat-fail { background: #3a1a1a; color: #f85149; }
.stat-total { background: #1a2a3a; color: #58a6ff; }
#filter { margin: 8px 12px; padding: 6px 10px; border: 1px solid #30363d; border-radius: 6px; background: #0d1117; color: #c9d1d9; font-size: 12px; outline: none; width: calc(100% - 24px); }
#filter:focus { border-color: #58a6ff; }
#case-list { flex: 1; overflow-y: auto; }
.case-item { padding: 7px 14px; cursor: pointer; border-bottom: 1px solid #21262d; font-size: 12px; display: flex; align-items: center; gap: 6px; }
.case-item:hover { background: #1c2129; }
.case-item.active { background: #1f2a37; border-left: 3px solid #58a6ff; padding-left: 11px; }
.case-item .icon { font-size: 14px; flex-shrink: 0; }
.case-id { overflow: hidden; text-overflow: ellipsis; }
.case-id .family { color: #8b949e; font-size: 11px; }
#main { flex: 1; display: flex; flex-direction: column; overflow: hidden; }
#tabs { display: flex; border-bottom: 1px solid #30363d; padding: 0 12px; gap: 2px; background: #161b22; }
.tab { padding: 8px 14px; cursor: pointer; border: none; background: none; color: #8b949e; font-size: 12px; border-bottom: 2px solid transparent; }
.tab:hover { color: #c9d1d9; }
.tab.active { color: #f0f6fc; border-bottom-color: #f78166; }
#content { flex: 1; overflow-y: auto; padding: 16px; }
#loading { color: #8b949e; padding: 40px; text-align: center; }
#images-panel { display: flex; flex-wrap: wrap; gap: 16px; }
.img-card { flex: 1; min-width: 280px; }
.img-card h3 { font-size: 11px; font-weight: 600; color: #8b949e; margin-bottom: 6px; text-transform: uppercase; letter-spacing: .5px; }
.img-card img { max-width: 100%; border: 1px solid #30363d; border-radius: 4px; }
.img-card .dims { font-size: 10px; color: #484f58; margin-top: 4px; }
table { width: 100%; border-collapse: collapse; font-size: 12px; }
th, td { padding: 5px 10px; text-align: right; border-bottom: 1px solid #21262d; }
th { text-align: right; color: #8b949e; font-weight: 600; white-space: nowrap; }
th:first-child, td:first-child { text-align: left; }
td.mono { font-family: 'SF Mono','Cascadia Code',monospace; font-size: 11px; }
.match { color: #3fb950; }
.mismatch { color: #f85149; }
.failure { color: #f85149; font-size: 12px; padding: 3px 0; }
.failure:before { content: "\2717 "; }
.empty { color: #484f58; padding: 40px; text-align: center; font-size: 13px; }
#detail-header { margin-bottom: 12px; }
#detail-header h2 { font-size: 15px; font-weight: 600; margin-bottom: 4px; word-break: break-all; }
#detail-header .meta { font-size: 11px; color: #484f58; }
#legend { padding: 6px 12px; border-top: 1px solid #30363d; font-size: 10px; color: #484f58; display: flex; gap: 12px; flex-wrap: wrap; }
#no-runs { padding: 40px; text-align: center; color: #484f58; }
@media(max-width:700px){body{flex-direction:column}#sidebar{width:100%;min-width:0;max-height:40vh}}
</style>
</head>
<body>
<div id="sidebar">
  <div id="run-selector"><select id="run-select"><option value="">(loading...)</option></select></div>
  <div id="stats"></div>
  <input id="filter" placeholder="Filter cases..." autofocus>
  <div id="case-list"></div>
  <div id="legend">
    <span>&#10003; exact</span><span style="color:#7ee060">~ equivalent</span><span style="color:#d2991d">&#9888; warning</span><span>&#10005; fail</span>
  </div>
</div>
<div id="main">
  <div id="tabs">
    <button class="tab active" data-panel="images">Images</button>
    <button class="tab" data-panel="geometry">Geometry</button>
    <button class="tab" data-panel="textlayout">Text Layout</button>
    <button class="tab" data-panel="shadow">Shadow</button>
    <button class="tab" data-panel="failures">Failures</button>
    <button class="tab" data-panel="raw">Raw JSON</button>
  </div>
  <div id="content">
    <div id="loading">Loading runs...</div>
    <div id="detail-header" style="display:none"><h2></h2><div class="meta"></div></div>
    <div id="images-panel"></div>
    <div id="geometry-panel" style="display:none"></div>
    <div id="textlayout-panel" style="display:none"></div>
    <div id="shadow-panel" style="display:none"></div>
    <div id="failures-panel" style="display:none"></div>
    <div id="raw-panel" style="display:none"></div>
  </div>
</div>

<script>
var API_BASE = '/api';
var CASES_BASE = '/cases';
var RUNS = [];
var CURRENT_RUN = null;
var state = {
    expectedManifest: null, actualManifest: null, profiles: null,
    comparisons: {}, expectedLayouts: {}, actualLayouts: {},
    caseMap: {}, activeCaseId: null
};

function getProfile(caseId) {
    if (!state.profiles) return null;
    var rules = state.profiles.caseProfileRules || [];
    for (var i = 0; i < rules.length; i++) {
        if (caseId.indexOf(rules[i].prefix) === 0) {
            var pid = rules[i].profileId;
            var profs = state.profiles.profiles || [];
            for (var j = 0; j < profs.length; j++) {
                if (profs[j].id === pid) return profs[j];
            }
        }
    }
    return null;
}

function getProfileLayoutTolerance(prof) {
    if (!prof) return null;
    // schema v6+: layoutTolerance.  v5-: tolerance.
    return prof.layoutTolerance || prof.tolerance || null;
}

async function loadRuns() {
    var resp = await fetch(API_BASE + '/runs');
    RUNS = await resp.json();
    try { state.profiles = await (await fetch(API_BASE + '/profiles')).json(); } catch(e) {}
    if (RUNS.length === 0) {
        document.getElementById('loading').outerHTML = '<div id="no-runs">No parity runs found in artifacts/visual-parity/xunit/</div>';
        return;
    }
    var sel = document.getElementById('run-select');
    sel.innerHTML = RUNS.map(function(r, i) {
        return '<option value="' + r.name + '">' + r.name + ' (' + r.actualCases + ' cases, ' + r.passCount + ' pass)</option>';
    }).join('');
    var latest = RUNS[RUNS.length - 1];
    sel.value = latest.name;
    selectRun(latest.name);
}

async function selectRun(runName) {
    CURRENT_RUN = runName;
    state.comparisons = {};
    state.expectedLayouts = {};
    state.actualLayouts = {};
    state.caseMap = {};
    state.activeCaseId = null;
    document.getElementById('loading').style.display = 'block';
    document.getElementById('detail-header').style.display = 'none';
    document.getElementById('images-panel').innerHTML = '';
    document.getElementById('geometry-panel').style.display = 'none';
    document.getElementById('textlayout-panel').style.display = 'none';
    document.getElementById('failures-panel').style.display = 'none';
    document.getElementById('raw-panel').style.display = 'none';
    document.getElementById('stats').innerHTML = '';
    document.getElementById('case-list').innerHTML = '';
    await loadRunData(runName);
}

async function loadRunData(runName) {
    try {
        var [em, am] = await Promise.all([
            fetch(API_BASE + '/run/' + runName + '/expected-manifest').then(function(r) { return r.json(); }),
            fetch(API_BASE + '/run/' + runName + '/actual-manifest').then(function(r) { return r.json(); })
        ]);
        state.expectedManifest = em;
        state.actualManifest = am;
    } catch(e) {
        document.getElementById('loading').textContent = 'Failed to load: ' + e.message;
        return;
    }
    document.getElementById('loading').style.display = 'none';
    var cases = am.cases || [];
    cases.forEach(function(c) { state.caseMap[c.id] = c; });
    var pass = 0, exact = 0, sem = 0, warn = 0, fail = 0;
    cases.forEach(function(c) {
        if (c.isMatch) pass++;
        else fail++;
        var k = c.comparisonKind || '';
        if (k === 'exactMatch') exact++;
        else if (k === 'semanticRasterEquivalent') sem++;
        else if (k === 'rasterWarning') warn++;
    });
    var stats = '<span class="stat stat-pass">PASS ' + pass + '</span>';
    if (sem > 0) stats += '<span class="stat stat-pass" style="background:#1a3520;color:#7ee060">~' + sem + '</span>';
    if (warn > 0) stats += '<span class="stat" style="background:#2a2010;color:#d2991d">WARN ' + warn + '</span>';
    if (fail > 0) stats += '<span class="stat stat-fail">FAIL ' + fail + '</span>';
    stats += '<span class="stat stat-total">TOTAL ' + cases.length + '</span>';
    document.getElementById('stats').innerHTML = stats;
    renderCaseList('');
    if (cases.length > 0) selectCase(cases[0].id);
}

var listEl = document.getElementById('case-list');
var filterEl = document.getElementById('filter');

function getStatusIcon(caseId) {
    var c = state.caseMap[caseId];
    if (!c || c.isMatch) {
        var k = c ? (c.comparisonKind || '') : '';
        if (k === 'semanticRasterEquivalent') return '<span class="icon" style="color:#7ee060">~</span>';
        if (k === 'rasterWarning') return '<span class="icon" style="color:#d2991d">\u26A0</span>';
        return '<span class="icon match">&#10003;</span>';
    }
    return '<span class="icon mismatch">&#10005;</span>';
}

function renderCaseList(filter) {
    var cases = state.actualManifest.cases || [];
    var ids = cases.map(function(c) { return c.id; });
    if (filter) {
        var lower = filter.toLowerCase();
        ids = ids.filter(function(id) { return id.toLowerCase().indexOf(lower) >= 0; });
    }
    var html = '';
    ids.forEach(function(id) {
        var family = id.split('/')[0];
        html += '<div class="case-item" data-id="' + id + '">' +
            getStatusIcon(id) +
            '<span class="case-id" title="' + id + '">' +
            '<span class="family">' + family + '/</span>' + id.substring(family.length + 1) +
            '</span></div>';
    });
    listEl.innerHTML = html || '<div class="empty">No cases match filter</div>';
}

filterEl.addEventListener('input', function() { renderCaseList(this.value); selectFirstVisible(); });
listEl.addEventListener('click', function(e) {
    var item = e.target.closest('.case-item');
    if (item) selectCase(item.dataset.id);
});

function selectFirstVisible() {
    var first = listEl.querySelector('.case-item');
    if (first) selectCase(first.dataset.id);
}

document.getElementById('tabs').addEventListener('click', function(e) {
    if (!e.target.classList.contains('tab')) return;
    document.querySelectorAll('.tab').forEach(function(t) { t.classList.remove('active'); });
    e.target.classList.add('active');
    var panel = e.target.dataset.panel;
    document.getElementById('images-panel').style.display = panel === 'images' ? 'flex' : 'none';
    ['geometry-panel','textlayout-panel','shadow-panel','failures-panel','raw-panel'].forEach(function(id) {
        document.getElementById(id).style.display = (id === panel + '-panel') ? '' : 'none';
    });
});

async function selectCase(caseId) {
    state.activeCaseId = caseId;
    document.querySelectorAll('.case-item').forEach(function(el) {
        el.classList.toggle('active', el.dataset.id === caseId);
    });
    var c = state.caseMap[caseId];
    document.getElementById('detail-header').style.display = '';
    var kind = c ? (c.comparisonKind || 'N/A') : 'N/A';
    var warnings = (c && c.warnings && c.warnings.length > 0) ? c.warnings.join('; ') : '';
    var kindCls = c && c.isMatch ? 'match' : 'mismatch';
    if (c && kind === 'semanticRasterEquivalent') kindCls = 'match';
    var meta = '<span class="' + kindCls + '">' + kind + '</span>';
    if (kind === 'semanticRasterEquivalent') meta += ' (acceptable, within tolerance)';
    if (kind === 'rasterWarning') meta += ' (passes with warnings)';
    if (warnings) meta += ' \u2022 ' + warnings;
    document.getElementById('detail-header').innerHTML =
        '<h2>' + caseId + '</h2><div class="meta">' + meta + '</div>';
    await loadCaseData(caseId);
    renderImages(caseId);
    renderGeometry(caseId);
    renderTextLayout(caseId);
    renderShadow(caseId);
    renderFailures(caseId);
    renderRaw(caseId);
}

async function loadCaseData(caseId) {
    try {
        var [comp, expLay, actLay] = await Promise.all([
            fetch(API_BASE + '/run/' + CURRENT_RUN + '/case/' + caseId + '/comparison').then(function(r) { return r.ok ? r.json() : null; }),
            fetch(API_BASE + '/run/' + CURRENT_RUN + '/case/' + caseId + '/expected-layout').then(function(r) { return r.ok ? r.json() : null; }),
            fetch(API_BASE + '/run/' + CURRENT_RUN + '/case/' + caseId + '/actual-layout').then(function(r) { return r.ok ? r.json() : null; })
        ]);
        state.comparisons[caseId] = comp;
        state.expectedLayouts[caseId] = expLay;
        state.actualLayouts[caseId] = actLay;
    } catch(e) {}
}

function renderImages(caseId) {
    var comp = state.comparisons[caseId];
    var px = comp ? comp.pixels : null;
    var c = state.caseMap[caseId];
    var kind = c ? (c.comparisonKind || '') : '';
    var isSemantic = kind === 'semanticRasterEquivalent' || kind === 'rasterWarning';
    var diffLabel;
    if (comp && !comp.isMatch) {
        diffLabel = '<h3 style="color:#f85149">FAIL &mdash; Diff (' + px.differentPixels + ' px, channels up to ' + px.maximumChannelDelta + ')</h3>';
    } else if (comp && isSemantic && px && px.differentPixels > 0) {
        diffLabel = '<h3 style="color:#7ee060">Acceptable &mdash; Diff (' + px.differentPixels + ' px, within semantic tolerance)</h3>';
    } else if (px && px.differentPixels > 0) {
        diffLabel = '<h3>Diff (' + px.differentPixels + ' px)</h3>';
    } else {
        diffLabel = '<h3 class="match">Exact match</h3>';
    }
    var imgs = '<div class="img-card"><h3>Expected (Compose)</h3>' +
        '<img src="' + CASES_BASE + '/' + CURRENT_RUN + '/expected/cases/' + caseId + '/image.png" alt="expected"></div>' +
        '<div class="img-card"><h3>Actual (Avalonia)</h3>' +
        '<img src="' + CASES_BASE + '/' + CURRENT_RUN + '/actual/cases/' + caseId + '/image.png" alt="actual"></div>' +
        '<div class="img-card">' + diffLabel +
        '<img src="' + CASES_BASE + '/' + CURRENT_RUN + '/actual/cases/' + caseId + '/diff.png" alt="diff" style="background:#0d1117"></div>';
    document.getElementById('images-panel').innerHTML = imgs;
}

function renderGeometry(caseId) {
    var exp = state.expectedLayouts[caseId];
    var act = state.actualLayouts[caseId];
    var prof = getProfile(caseId);
    var tol = getProfileLayoutTolerance(prof);
    var geomTol = tol ? (tol.geometryPx != null ? tol.geometryPx : 0) : 0;
    if (!exp) { document.getElementById('geometry-panel').innerHTML = '<div class="empty">No expected layout data</div>'; return; }
    var expMap = {}, actMap = {};
    (exp.nodes || []).forEach(function(n) { expMap[n.id] = n; });
    if (act) (act.nodes || []).forEach(function(n) { actMap[n.id] = n; });
    var rows = '';
    (exp.nodes || []).forEach(function(n) {
        var a = actMap[n.id];
        var eb = n.boundsInRootPx || {};
        var ab = a && a.boundsInRootPx ? a.boundsInRootPx : {};
        var f = function(v) { return v != null ? v.toFixed(1) : '-'; };
        var ok = function(d) { return a && d !== '-' && Math.abs(parseFloat(d)) <= geomTol; };
        var dl = a && eb.left != null && ab.left != null ? (ab.left - eb.left).toFixed(1) : '-';
        var dt = a && eb.top != null && ab.top != null ? (ab.top - eb.top).toFixed(1) : '-';
        var dr = a && eb.right != null && ab.right != null ? (ab.right - eb.right).toFixed(1) : '-';
        var db = a && eb.bottom != null && ab.bottom != null ? (ab.bottom - eb.bottom).toFixed(1) : '-';
        var clsL = ok(dl) ? 'match' : (a ? 'mismatch' : 'mismatch');
        var clsT = ok(dt) ? 'match' : (a ? 'mismatch' : 'mismatch');
        var clsR = ok(dr) ? 'match' : (a ? 'mismatch' : 'mismatch');
        var clsB = ok(db) ? 'match' : (a ? 'mismatch' : 'mismatch');
        rows += '<tr><td>'+n.id+'</td>' +
            '<td class="mono">'+f(eb.left)+','+f(eb.top)+' -> '+f(eb.right)+','+f(eb.bottom)+'</td>' +
            '<td class="mono">'+f(ab.left)+','+f(ab.top)+' -> '+f(ab.right)+','+f(ab.bottom)+'</td>' +
            '<td class="mono"><span class="'+clsL+'">'+dl+'</span> / <span class="'+clsT+'">'+dt+'</span> / <span class="'+clsR+'">'+dr+'</span> / <span class="'+clsB+'">'+db+'</span></td></tr>';
    });
    document.getElementById('geometry-panel').innerHTML =
        '<table><thead><tr><th>Node</th><th>Expected (px)</th><th>Actual (px)</th><th>Delta L/T/R/B (tol='+geomTol.toFixed(1)+'px)</th></tr></thead><tbody>'+rows+'</tbody></table>';
}

function renderTextLayout(caseId) {
    var exp = state.expectedLayouts[caseId];
    var act = state.actualLayouts[caseId];
    var prof = getProfile(caseId);
    var lt = getProfileLayoutTolerance(prof);
    var textTol = lt ? (lt.textPx != null ? lt.textPx : 0) : 0;
    var baselineTol = lt ? (lt.textBaselinePx != null ? lt.textBaselinePx : textTol) : 0;
    var lineTol = lt ? (lt.textLineBoundsPx != null ? lt.textLineBoundsPx : textTol) : 0;
    var textNodes = (exp&&exp.nodes||[]).filter(function(n){return n.textLayout;});
    if (textNodes.length===0) {
        document.getElementById('textlayout-panel').innerHTML='<div class="empty">No text layout data</div>'; return;
    }
    var h = '';
    textNodes.forEach(function(en) {
        var an = act?(act.nodes||[]).find(function(n){return n.id===en.id;}):null;
        var et=en.textLayout, at=an?an.textLayout:null;
        h+='<h3 style="margin:12px 0 6px">Node: <code>'+en.id+'</code> &mdash; '+(et.text||'')+'</h3>';
        h+='<table><thead><tr><th>Metric</th><th>Expected</th><th>Actual</th><th>Delta (char tol='+textTol.toFixed(1)+'px, baseline tol='+baselineTol.toFixed(1)+'px)</th></tr></thead><tbody>';
        var qc=function(l,ev,av,tol){
            var d=(ev!=null&&av!=null)?(av-ev):null;
            var ds = d !== null ? d.toFixed(3) : '-';
            var ok = d !== null && Math.abs(d) <= tol;
            h+='<tr class="'+(ok?'match':'mismatch')+'"><td>'+l+'</td><td class="mono">'+(ev!=null?ev.toFixed(3):'-')+'</td><td class="mono">'+(av!=null?av.toFixed(3):'-')+'</td><td class="mono">'+ds+'</td></tr>';
        };
        qc('Line count',et.lineCount,at?at.lineCount:null,0);
        qc('First baseline',et.firstBaselineInRootPx,at?at.firstBaselineInRootPx:null,baselineTol);
        qc('Last baseline',et.lastBaselineInRootPx,at?at.lastBaselineInRootPx:null,baselineTol);
        h+='</tbody></table>';
        if(et.lines&&et.lines.length>0){
            h+='<h4 style="margin:10px 0 4px;color:#8b949e;font-size:11px">Line Local Bounds (tol='+lineTol.toFixed(1)+'px)</h4>';
            h+='<table><thead><tr><th>Line</th><th>Expected (local px)</th><th>Actual (local px)</th><th>Delta LTRB</th></tr></thead><tbody>';
            (et.lines||[]).forEach(function(el,i){
                var al=(at&&at.lines)?at.lines[i]:null;
                if(!al){h+='<tr class="mismatch"><td>'+i+'</td><td colspan=3>Missing</td></tr>';return;}
                var elb=el.boundsInTextPx||{};
                var alb=al.boundsInTextPx||{};
                var dl=alb.left!=null&&elb.left!=null?(alb.left-elb.left).toFixed(1):'-';
                var dt=alb.top!=null&&elb.top!=null?(alb.top-elb.top).toFixed(1):'-';
                var dr=alb.right!=null&&elb.right!=null?(alb.right-elb.right).toFixed(1):'-';
                var db=alb.bottom!=null&&elb.bottom!=null?(alb.bottom-elb.bottom).toFixed(1):'-';
                var ok=function(d){return d==='-'||Math.abs(parseFloat(d))<=lineTol;};
                h+='<tr><td>'+i+'</td>' +
                    '<td class="mono">'+elb.left.toFixed(1)+','+elb.top.toFixed(1)+' -> '+elb.right.toFixed(1)+','+elb.bottom.toFixed(1)+'</td>' +
                    '<td class="mono">'+alb.left.toFixed(1)+','+alb.top.toFixed(1)+' -> '+alb.right.toFixed(1)+','+alb.bottom.toFixed(1)+'</td>' +
                    '<td class="mono"><span class="'+(ok(dl)?'match':'mismatch')+'">'+dl+'</span> / <span class="'+(ok(dt)?'match':'mismatch')+'">'+dt+'</span> / <span class="'+(ok(dr)?'match':'mismatch')+'">'+dr+'</span> / <span class="'+(ok(db)?'match':'mismatch')+'">'+db+'</span></td></tr>';
            });
            h+='</tbody></table>';
        }
        if(et.characterBoxes&&et.characterBoxes.length>0){
            h+='<h4 style="margin:10px 0 4px;color:#8b949e;font-size:11px">Characters ('+et.characterBoxes.length+') (tol='+textTol.toFixed(1)+'px)</h4>';
            h+='<table><thead><tr><th>Offset</th><th>Code</th><th>Expected Bounds</th><th>Actual Bounds</th><th>Max Delta (tol='+textTol.toFixed(1)+'px)</th></tr></thead><tbody>';
            (et.characterBoxes||[]).forEach(function(eb,i){
                var ab=(at&&at.characterBoxes)?at.characterBoxes[i]:null;
                if(!ab){h+='<tr class="mismatch"><td>'+eb.utf16Offset+'</td><td>U+'+eb.codeUnit+'</td><td colspan=3 class="mismatch">Missing</td></tr>';return;}
                var md=Math.max(Math.abs(ab.boundsInRootPx.left-eb.boundsInRootPx.left),Math.abs(ab.boundsInRootPx.top-eb.boundsInRootPx.top),Math.abs(ab.boundsInRootPx.right-eb.boundsInRootPx.right),Math.abs(ab.boundsInRootPx.bottom-eb.boundsInRootPx.bottom));
                var ok = md <= textTol;
                h+='<tr class="'+(ok?'match':'mismatch')+'"><td>'+eb.utf16Offset+'</td><td>U+'+eb.codeUnit+'</td><td class="mono">'+eb.boundsInRootPx.left.toFixed(1)+','+eb.boundsInRootPx.top.toFixed(1)+'..'+eb.boundsInRootPx.right.toFixed(1)+','+eb.boundsInRootPx.bottom.toFixed(1)+'</td><td class="mono">'+ab.boundsInRootPx.left.toFixed(1)+','+ab.boundsInRootPx.top.toFixed(1)+'..'+ab.boundsInRootPx.right.toFixed(1)+','+ab.boundsInRootPx.bottom.toFixed(1)+'</td><td class="mono '+(ok?'match':'mismatch')+'">'+md.toFixed(2)+'</td></tr>';
            });
            h+='</tbody></table>';
        }
    });
    document.getElementById('textlayout-panel').innerHTML=h;
}

function renderShadow(caseId) {
    var exp = state.expectedLayouts[caseId];
    var act = state.actualLayouts[caseId];
    var expNodes = (exp&&exp.nodes||[]).filter(function(n){return n.shadow;});
    var actNodes = (act&&act.nodes||[]).filter(function(n){return n.shadow;});
    var allIds = {};
    expNodes.forEach(function(n){allIds[n.id]=true;});
    actNodes.forEach(function(n){allIds[n.id]=true;});
    if (Object.keys(allIds).length===0) {
        document.getElementById('shadow-panel').innerHTML='<div class="empty">No shadow data for this case</div>';
        return;
    }
    var prof = getProfile(caseId);
    var lt = getProfileLayoutTolerance(prof);
    var tol = lt ? (lt.geometryPx != null ? lt.geometryPx : 0.5) : 0.5;
    var h = '';
    Object.keys(allIds).forEach(function(nid) {
        var en = expNodes.find(function(n){return n.id===nid;});
        var an = actNodes.find(function(n){return n.id===nid;});
        var es = en?en.shadow:null, as = an?an.shadow:null;
        if (!es&&!as) return;
        h+='<h3 style="margin:12px 0 6px">Node: <code>'+nid+'</code></h3>';
        var q=function(l,ev,av,t){
            var ok='<span class="match">OK</span>';
            var exp='-', act='-';
            if(es){exp=String(ev!=null?ev:'-');}
            if(as){act=String(av!=null?av:'-');}
            if(ev!=null&&av!=null){
                var d=Math.abs(ev-av);
                ok=d<=t?'<span class="match">'+d.toFixed(3)+'</span>':'<span class="mismatch">'+d.toFixed(3)+'</span>';
            } else if(!es||!as) {
                ok='<span class="mismatch">MISSING</span>';
            }
            h+='<tr><td>'+l+'</td><td class="mono">'+exp+'</td><td class="mono">'+act+'</td><td>'+ok+'</td></tr>';
        };
        var qs=function(l,ev,av){
            var ok='<span class="match">OK</span>';
            if(ev!==av){ok='<span class="mismatch">'+av+'</span>';}
            h+='<tr><td>'+l+'</td><td class="mono">'+(es?ev:'-')+'</td><td class="mono">'+(as?av:'-')+'</td><td>'+ok+'</td></tr>';
        };
        h+='<table><thead><tr><th>Property</th><th>Expected</th><th>Actual</th><th>Delta (tol='+tol.toFixed(1)+'px)</th></tr></thead><tbody>';
        q('Elevation (px)',es?es.elevationPx:null,as?as.elevationPx:null,tol);
        q('Opacity',es?es.opacity:null,as?as.opacity:null,0.001);
        q('Ambient alpha',es?es.ambientAlpha:null,as?as.ambientAlpha:null,0.001);
        q('Spot alpha',es?es.spotAlpha:null,as?as.spotAlpha:null,0.001);
        q('Light radius (px)',es?es.lightRadiusPx:null,as?as.lightRadiusPx:null,tol);
        q('Light position X',es&&es.lightPositionPx?es.lightPositionPx.x:null,as&&as.lightPositionPx?as.lightPositionPx.x:null,tol);
        q('Light position Y',es&&es.lightPositionPx?es.lightPositionPx.y:null,as&&as.lightPositionPx?as.lightPositionPx.y:null,tol);
        q('Light position Z',es&&es.lightPositionPx?es.lightPositionPx.z:null,as&&as.lightPositionPx?as.lightPositionPx.z:null,tol);
        qs('Shape kind',es?es.shapeKind:'-',as?as.shapeKind:'-');
        qs('Skia behavior',es?es.skiaBehaviorVersion:'-',as?as.skiaBehaviorVersion:'-');
        qs('Ambient color',es?es.ambientColorArgb:'-',as?as.ambientColorArgb:'-');
        qs('Spot color',es?es.spotColorArgb:'-',as?as.spotColorArgb:'-');
        qs('Transparent occluder',es?String(es.transparentOccluder):'-',as?String(as.transparentOccluder):'-');
        h+='</tbody></table>';
    });
    document.getElementById('shadow-panel').innerHTML=h;
}

function renderFailures(caseId) {
    var comp = state.comparisons[caseId];
    if (!comp || !comp.failures || comp.failures.length===0) {
        document.getElementById('failures-panel').innerHTML='<div class="empty" style="color:#3fb950">No failures</div>'; return;
    }
    var h = '<div style="padding:4px 0 8px;font-size:12px;color:#8b949e">'+comp.failures.length+' failure(s) &bull; Profile: <code>'+comp.profileId+'</code>';
    if(comp.pixels)h+=' &bull; Raster: '+comp.pixels.differentPixels+'/'+comp.pixels.totalPixels+' px';
    h+='</div>';
    comp.failures.forEach(function(f){h+='<div class="failure">'+f+'</div>';});
    document.getElementById('failures-panel').innerHTML=h;
}

function renderRaw(caseId) {
    var c=state.comparisons[caseId], e=state.expectedLayouts[caseId], a=state.actualLayouts[caseId];
    document.getElementById('raw-panel').innerHTML=
        '<details open><summary style="font-size:12px;color:#8b949e;cursor:pointer">comparison.json</summary><pre style="font-size:11px;max-height:300px;overflow:auto;background:#161b22;padding:8px;border-radius:4px">'+JSON.stringify(c,null,2)+'</pre></details>'+
        '<details style="margin-top:8px"><summary style="font-size:12px;color:#8b949e;cursor:pointer">expected layout.json</summary><pre style="font-size:11px;max-height:300px;overflow:auto;background:#161b22;padding:8px;border-radius:4px">'+JSON.stringify(e,null,2)+'</pre></details>'+
        '<details style="margin-top:8px"><summary style="font-size:12px;color:#8b949e;cursor:pointer">actual layout.json</summary><pre style="font-size:11px;max-height:300px;overflow:auto;background:#161b22;padding:8px;border-radius:4px">'+JSON.stringify(a,null,2)+'</pre></details>';
}

document.addEventListener('keydown',function(e){
    if(e.key==='ArrowDown'||e.key==='ArrowUp'){
        e.preventDefault();
        var items=listEl.querySelectorAll('.case-item');
        var idx=Array.from(items).findIndex(function(el){return el.dataset.id===state.activeCaseId;});
        if(e.key==='ArrowDown')idx=Math.min(idx+1,items.length-1);
        else idx=Math.max(idx-1,0);
        if(idx>=0&&items[idx]){selectCase(items[idx].dataset.id);items[idx].scrollIntoView({block:'nearest'});}
    }
});

document.getElementById('run-select').addEventListener('change',function(){selectRun(this.value);});
loadRuns();
</script>
</body>
</html>"""


@app.route("/")
def index():
    return INDEX_HTML


@app.route("/api/runs")
def api_runs():
    runs = []
    for d in sorted(ARTIFACTS.glob("*"), key=lambda p: p.stat().st_mtime if p.is_dir() else 0):
        if not d.is_dir():
            continue
        am = d / "actual" / "manifest.json"
        if not am.exists():
            continue
        try:
            data = json.loads(am.read_text("utf-8"))
            cases = data.get("cases", [])
            runs.append({
                "name": d.name,
                "actualCases": len(cases),
                "passCount": sum(1 for c in cases if c.get("isMatch")),
            })
        except Exception:
            continue
    return jsonify(runs)


@app.route("/api/profiles")
def api_profiles():
    if not PROFILES_PATH.exists():
        return jsonify({})
    return jsonify(json.loads(PROFILES_PATH.read_text("utf-8")))


@app.route("/api/run/<run_name>/expected-manifest")
def api_expected_manifest(run_name):
    return _send_json(run_name, "expected/manifest.json")


@app.route("/api/run/<run_name>/actual-manifest")
def api_actual_manifest(run_name):
    return _send_json(run_name, "actual/manifest.json")


@app.route("/api/run/<run_name>/case/<path:case_id>/comparison")
def api_case_comparison(run_name, case_id):
    return _send_json(run_name, f"actual/cases/{case_id}/comparison.json")


@app.route("/api/run/<run_name>/case/<path:case_id>/expected-layout")
def api_case_expected_layout(run_name, case_id):
    return _send_json(run_name, f"expected/cases/{case_id}/layout.json")


@app.route("/api/run/<run_name>/case/<path:case_id>/actual-layout")
def api_case_actual_layout(run_name, case_id):
    return _send_json(run_name, f"actual/cases/{case_id}/layout.json")


def _send_json(run_name, rel_path):
    p = ARTIFACTS / run_name / rel_path
    if not p.exists():
        return jsonify(None)
    return jsonify(json.loads(p.read_text("utf-8")))


@app.route("/cases/<path:filepath>")
def serve_case_file(filepath):
    return send_from_directory(VISUAL_PARITY_DIR, "xunit/" + filepath)


def main():
    port = PORT
    args = sys.argv[1:]
    i = 0
    while i < len(args):
        if args[i] == "--port" and i + 1 < len(args):
            port = int(args[i + 1])
            i += 2
        else:
            i += 1

    runs = [d.name for d in sorted(ARTIFACTS.glob("*")) if d.is_dir() and (d / "actual/manifest.json").exists()]
    print(f"Found {len(runs)} run(s) in {ARTIFACTS.relative_to(ROOT)}")
    if runs:
        print(f"Latest: {runs[-1]}")

    url = f"http://127.0.0.1:{port}/"
    print(f"Serving at {url} (Ctrl+C to stop)")

    webbrowser.open(url)
    app.run(host="127.0.0.1", port=port, debug=False)


if __name__ == "__main__":
    main()
