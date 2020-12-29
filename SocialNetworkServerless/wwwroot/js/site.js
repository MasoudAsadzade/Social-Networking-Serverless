$(function () {
    $('ul#users-list').on('click', 'li', function () {
        var username = $("input[type=hidden].username", $(this)).val();
        var input = $('#chat-message');

        var text = input.val();
        if (text.startsWith("/")) {
            text = text.split(")")[1];
        }

        text = "/private(" + username + ") " + text.trim();
        input.val(text);
        input.change();
        input.focus();
    });

    $(".alert .close").on('click', function () {
        $(this).parent().hide();
    });
});