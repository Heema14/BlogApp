
$(function () {
    $("#searchInput").autocomplete({
        source: function (request, response) {
            // ✅ Show spinner
            $("#searchInput").addClass("autocomplete-loading");

            $.ajax({
                url: searchSuggestionsUrl,

                data: {
                    term: request.term,
                    filterBy: $('select[name="filterBy"]').val()
                },
                success: function (data) {
                    response(data);
                },
                complete: function () {
                    // ✅ Hide spinner
                    $("#searchInput").removeClass("autocomplete-loading");
                }
            });
        },
        minLength: 2
    });
});
$('#clearSearch').on('click', function () {
    $('#searchInput').val('');
    $('#searchInput').focus();
});