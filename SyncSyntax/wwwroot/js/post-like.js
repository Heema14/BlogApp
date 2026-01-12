
(function () {
    let postConnection = null;

    function stopConnection() {
        if (postConnection) {
            postConnection.stop();
            postConnection = null;
        }
    }

    window.setupPostLikeHub = function (postId) {
        stopConnection();

        postConnection = new signalR.HubConnectionBuilder()
            .withUrl(`/postHub?postId=${postId}`)
            .build();

        postConnection.on("ReceiveLike", function (postId, likesCount, userId) {
            $(`.like-btn[data-postid="${postId}"] .likes-count`).text(likesCount);
        });

        postConnection.start().catch(err => console.error(err.toString()));
    };

    window.toggleLike = function (postId) {
        $.ajax({
            url: window.postLikeEndpoints.likeUrl,
            type: "POST",
            data: { postId },
            success: function (response) {
                if (!response.success) return alert(response.message);

                const $allButtons = $(`.like-btn[data-postid="${postId}"]`);
                $allButtons.find(".likes-count").text(response.likesCount);
                $allButtons.find(".like-icon").css("color", response.userLiked ? "red" : "gray");
            },
            error: function () {
                alert("An error occurred while processing your request.");
            }
        });
    };

    $(document).off("click", ".like-btn").on("click", ".like-btn", function () {
        const postId = $(this).data("postid");
        window.toggleLike(postId);
    });

    $("#postModal").on("hidden.bs.modal", function () {
        stopConnection();
        $(this).find(".modal-body").html("");
    });
})();
