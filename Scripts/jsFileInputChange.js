/* ============================================================
   jsFileInputChange — feedback visual para campos de upload
   Substitui custom-file do BS4. Uso:
     <input type="file" id="meuArquivo" onchange="jsFileInputChange(this)">
     <div class="form-text" id="meuArquivo_info"></div>
   ============================================================ */
function jsFileInputChange(input) {
    try {
        var infoEl = document.getElementById(input.id + '_info');
        if (!infoEl) return;

        if (!input.files || input.files.length === 0) {
            infoEl.innerHTML = '';
            infoEl.className = 'form-text';
            return;
        }

        var lines = [];
        var totalBytes = 0;

        for (var i = 0; i < input.files.length; i++) {
            var file = input.files[i];
            totalBytes += file.size;
            var kb  = (file.size / 1024).toFixed(1);
            var mb  = (file.size / 1048576).toFixed(2);
            var sz  = parseFloat(mb) >= 1 ? mb + ' MB' : kb + ' KB';
            lines.push('<i class="fa-solid fa-paperclip me-1" aria-hidden="true"></i>'
                + '<strong>' + file.name + '</strong>'
                + ' <span class="text-muted">(' + sz + ')</span>');
        }

        if (input.files.length > 1) {
            var totalMb  = (totalBytes / 1048576).toFixed(2);
            var totalKb  = (totalBytes / 1024).toFixed(1);
            var totalSz  = parseFloat(totalMb) >= 1 ? totalMb + ' MB' : totalKb + ' KB';
            lines.push('<span class="text-muted">'
                + input.files.length + ' arquivos — total: ' + totalSz + '</span>');
        }

        infoEl.innerHTML  = lines.join('<br>');
        infoEl.className  = 'form-text text-success mt-1';

    } catch (err) {
        console.error('[jsFileInputChange] ' + err.message);
    }
}
