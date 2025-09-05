$(document).ready(function () {
    // إنشاء اتصال SignalR واحد فقط
    var connection = new signalR.HubConnectionBuilder()
        .withUrl("/postLikeHub")
        .build();

    // استقبال التحديثات من السيرفر
    connection.on("ReceiveLike", function (postId, likesCount) {
        $('#likesCount-' + postId).text(likesCount);
    });

    // بدء الاتصال
    connection.start()
        .then(function () {
            // بعد ما يشتغل الاتصال، ندخل كل بوست ظاهر بالصفحة على الجروب
            $('.like-btn').each(function () {
                var postId = $(this).data('postid');
                connection.invoke("JoinGroup", postId)
                    .catch(err => console.error(err.toString()));
            });
        })
        .catch(err => console.error(err.toString()));

    // تفعيل اللايك عند الضغط
    $(document).on('click', '.like-btn', function () {
        var postId = $(this).data('postid');

        $.ajax({
            url: '/ContentCreator/Post/Like',
            type: 'POST',
            data: { postId: postId },
            success: function (response) {
                if (response.success) {
                    $('#likesCount-' + postId).text(response.likesCount);
                    $('#likeIcon-' + postId).css('color', response.userLiked ? 'red' : 'gray');
                } else {
                    alert(response.message);
                }
            },
            error: function () {
                alert('حدث خطأ أثناء تنفيذ الطلب.');
            }
        });
    });
});
