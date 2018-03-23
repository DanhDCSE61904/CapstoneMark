
function CreateEmptyDataTable(id) {
    var table = $(id).dataTable({
        // "sScrollX": "100%",
        // "bScrollCollapse": true,
        "bSort": false,
        "oLanguage": {
            "sSearchPlaceholder": "",
            "sSearch": "Tìm kiếm:",
            "sZeroRecords": "Không có dữ liệu phù hợp",
            "sInfo": "Hiển thị từ _START_ đến _END_ trên tổng số _TOTAL_ dòng",
            "sEmptyTable": "Không có dữ liệu",
            "sInfoFiltered": " - lọc ra từ _MAX_ dòng",
            "sLengthMenu": "Hiển thị _MENU_ dòng",
            "sProcessing": "Đang xử lý...",
            "oPaginate": {
                "sNext": "<i class='fa fa-chevron-right'></i>",
                "sPrevious": "<i class='fa fa-chevron-left'></i>"
            }
        },
        "bAutoWidth": false,
    });

    return table;
}

function ShowTableColumn(table, colNum) {
    $(table).dataTable().fnSetColumnVis(colNum, true);
}

function HideTableColumn(table, colNum) {
    $(table).dataTable().fnSetColumnVis(colNum, false);
}