(function () {
    function getCellText(row, colIndex) {
        var cell = row.querySelectorAll('td')[colIndex];
        return cell ? cell.textContent.trim().toLowerCase() : '';
    }

    function getCellSortValue(row, colIndex) {
        var cell = row.querySelectorAll('td')[colIndex];
        if (!cell) return '';
        var sortValue = cell.dataset.sortValue;
        return sortValue !== undefined ? sortValue : cell.textContent.trim().toLowerCase();
    }

    function applyFilters(table) {
        var inputs = table.querySelectorAll('.table-filter-input');
        var rows = Array.from(table.querySelector('tbody').querySelectorAll('tr'));
        rows.forEach(function (row) {
            var show = true;
            inputs.forEach(function (input) {
                var colIndex = parseInt(input.dataset.colIndex);
                var filterVal = input.value.trim().toLowerCase();
                if (filterVal && !getCellText(row, colIndex).includes(filterVal)) {
                    show = false;
                }
            });
            row.style.display = show ? '' : 'none';
        });
    }

    function sortTable(table, colIndex, th) {
        var tbody = table.querySelector('tbody');
        var allHeaders = table.querySelectorAll('thead tr:first-child th');
        var dir;
        if (table._sortState.colIndex === colIndex) {
            dir = table._sortState.dir === 'asc' ? 'desc' : table._sortState.dir === 'desc' ? '' : 'asc';
        } else {
            dir = 'asc';
        }

        allHeaders.forEach(function (h) {
            var icon = h.querySelector('.sort-icon i');
            if (icon) {
                icon.className = 'fas fa-sort text-muted opacity-50';
            }
        });

        table._sortState = { colIndex: colIndex, dir: dir };

        var icon = th.querySelector('.sort-icon i');
        var rows = Array.from(tbody.querySelectorAll('tr'));

        if (dir === '') {
            if (icon) icon.className = 'fas fa-sort text-muted opacity-50';
            rows.sort(function (a, b) {
                return parseInt(a.dataset.originalIndex) - parseInt(b.dataset.originalIndex);
            });
        } else {
            if (icon) icon.className = dir === 'asc' ? 'fas fa-sort-up' : 'fas fa-sort-down';
            rows.sort(function (a, b) {
                var aText = getCellSortValue(a, colIndex);
                var bText = getCellSortValue(b, colIndex);
                var aNum = parseFloat(aText.replace(/[^0-9.-]/g, ''));
                var bNum = parseFloat(bText.replace(/[^0-9.-]/g, ''));
                var cmp;
                if (!isNaN(aNum) && !isNaN(bNum) && aText !== '' && bText !== '') {
                    cmp = aNum - bNum;
                } else {
                    var aDate = new Date(aText);
                    var bDate = new Date(bText);
                    if (!isNaN(aDate.getTime()) && !isNaN(bDate.getTime()) && aText !== '' && bText !== '') {
                        cmp = aDate - bDate;
                    } else {
                        cmp = aText.localeCompare(bText);
                    }
                }
                return dir === 'asc' ? cmp : -cmp;
            });
        }

        rows.forEach(function (row) { tbody.appendChild(row); });
        applyFilters(table);
    }

    function initSortableTable(table) {
        var headers = table.querySelectorAll('thead tr:first-child th');
        var tbody = table.querySelector('tbody');
        Array.from(tbody.querySelectorAll('tr')).forEach(function (row, i) {
            row.dataset.originalIndex = i;
        });

        table._sortState = { colIndex: -1, dir: '' };

        var filterRow = document.createElement('tr');
        filterRow.classList.add('table-filter-row');

        headers.forEach(function (th, colIndex) {
            var td = document.createElement('td');
            if (th.textContent.trim() !== 'Actions') {
                var input = document.createElement('input');
                input.type = 'text';
                input.classList.add('form-control', 'form-control-sm', 'table-filter-input');
                input.placeholder = 'Filter...';
                input.dataset.colIndex = colIndex;
                input.addEventListener('input', function () { applyFilters(table); });
                td.appendChild(input);

                th.classList.add('sortable-th');
                th.dataset.colIndex = colIndex;

                var icon = document.createElement('span');
                icon.classList.add('sort-icon', 'ms-1');
                icon.innerHTML = '<i class="fas fa-sort text-muted opacity-50"></i>';
                th.appendChild(icon);

                th.addEventListener('click', function () { sortTable(table, colIndex, th); });
            }
            filterRow.appendChild(td);
        });

        table.querySelector('thead').appendChild(filterRow);
    }

    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('table[data-sortable]').forEach(initSortableTable);
    });
}());
