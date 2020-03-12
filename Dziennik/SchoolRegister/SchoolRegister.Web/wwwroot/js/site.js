$("#subjects-filter-input").change(function() {
    const filterValue = $("#subjects-filter-input").val();
        $.get("/Subject/Index", $.param({ filterValue: filterValue }), function(resultData) {
            $(".subjects-table-data").html(resultData);
        });
});