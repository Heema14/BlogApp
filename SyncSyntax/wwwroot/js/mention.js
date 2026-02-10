document.addEventListener('DOMContentLoaded', function () {
    ClassicEditor
        .create(document.querySelector('#editor'), {
            mention: {
                feeds: [
                    {
                        marker: '@',  
                        feed: function (query) {
                            const queryText = query.substring(1);  

                            return fetch('/ContentCreator/Post/search?query=' + queryText)
                                .then(response => response.json())
                                .then(data => {
                                    return data.map(user => {
                                        return {
                                            id: user.id,
                                            text: user.username
                                        };
                                    });
                                });
                        }

                    }
                ]
            }
        })
        .catch(error => {
            console.error("Error initializing CKEditor:", error); 
        });
});
