
document.addEventListener('DOMContentLoaded', function () {
    //Start Connection
    var connection = new signalR.HubConnectionBuilder()
        .withUrl("/chathub")
        .build();


    connection.start()
        .then(() => {
            console.log("SignalR connected");

        })
        .catch(err => console.error("SignalR connection failed:", err.toString()));

        //Recieve Message

    connection.on("ReceiveMessage", function (sender, message, messageId, sentAt, isRead, isPinned, reactions) {
        console.log("Message received from SignalR:", sender, message);

        try {
            var messageContainer = document.getElementById("messagesContainer");
            console.log('messagesContainer:', messageContainer);

            var messageElement = document.createElement("div");

            var currentUserId = document.getElementById("senderId").value;

            var isSent = sender === currentUserId;

            messageElement.classList.add("message");
            messageElement.classList.add(isSent ? "sent" : "received");

            messageElement.setAttribute('data-id', messageId);
            messageElement.setAttribute('data-sender-id', sender);
            messageElement.innerHTML = `
    <input type="checkbox" class="message-select-checkbox" style="display:none;" />
    <div class="message-header">
        <div class="dropdown message-options">
            <button class="dots-btn text-black" data-bs-toggle="dropdown" aria-expanded="false">⋮</button>
            <ul class="dropdown-menu" style="direction: rtl;">
                ${isSent ? `
                    <li><a class="dropdown-item edit-message" data-id="${messageId}" href="#">Edit</a></li>
                    <li><a class="dropdown-item delete-message" data-id="${messageId}" data-scope="me" href="#">Delete for me</a></li>
                    <li><a class="dropdown-item delete-message" data-id="${messageId}" data-scope="all" href="#">Delete for everyone</a></li>
                    <li><a class="dropdown-item info-message" data-id="${messageId}" href="#">Info</a></li>
                ` : `
                    <li><a class="dropdown-item pin-message" data-id="${messageId}" href="#">${isPinned ? "Unpin" : "Pin"}</a></li>
                    <li><a class="dropdown-item copy-message" data-id="${messageId}" href="#">Copy</a></li>
                    <li><a class="dropdown-item delete-message" data-id="${messageId}" data-scope="me" href="#">Delete for me</a></li>
                `}
            </ul>
        </div>
    </div>
    <p>${message}</p>

        <div class="reaction-summary" data-loaded="false"></div>


    <div class="reaction-bar">
        <span class="emoji-option">👍</span>
        <span class="emoji-option">❤️</span>
        <span class="emoji-option">😂</span>
        <span class="emoji-option">😮</span>
        <span class="emoji-option">😢</span>
        <span class="emoji-option emoji-plus">➕</span>
       
    </div>

    <small>
        ${new Date(sentAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
        ${isSent && isRead ? '<span title="Read">✅</span>' : ''}
    </small>
`;


            messageContainer.appendChild(messageElement);
            //For Get Reaction
            // بعد messageElement.appendChild(messageElement);
 
                fetch(`/ContentCreator/Messages/GetReactionsForMessage/${messageId}`)
                    .then(response => response.json())
                    .then(data => {
                        const summaryDiv = messageElement.querySelector(".reaction-summary");
                        summaryDiv.innerHTML = "";  

                        const reactionGroups = {};
                        data.forEach(r => {
                            if (!reactionGroups[r.reaction]) reactionGroups[r.reaction] = 0;
                            reactionGroups[r.reaction]++;
                        });

                        for (let emoji in reactionGroups) {
                            const span = document.createElement("span");
                            span.textContent = `${emoji} ${reactionGroups[emoji]}`;
                            span.className = "reaction-count";
                            summaryDiv.appendChild(span);
                        }

                    })
                    .catch(err => console.error("Error fetching reactions:", err));
            bootstrap.Dropdown.getOrCreateInstance(messageElement.querySelector('[data-bs-toggle="dropdown"]'));
            messageContainer.scrollTop = messageContainer.scrollHeight;

        } catch (error) {
            console.error("Error appending message:", error);
        }
    });

    //Send Button
    document.getElementById("sendButton").addEventListener("click", function () {
        var sender = document.getElementById("senderId").value.trim();
        var receiver = document.getElementById("receiverId").value.trim();
        var messageContent = document.getElementById("messageInput").value.trim();

        if (!messageContent || !sender || !receiver) {
            alert("Please provide Sender, Receiver, and Message.");
            return;
        }

        connection.invoke("SendMessage", sender, receiver, messageContent)
            .catch(err => console.error(err));

        document.getElementById("messageInput").value = "";
    });

    let editingMessageId = null;
    //Delete Message
    $(document).on('click', '.delete-message', function (e) {
        e.preventDefault();
        const messageId = $(this).data('id');
        const deleteScope = $(this).data('scope');
        const messageElement = $(this).closest('.message');

        let confirmText = deleteScope === "all"
            ? "Are you sure you want to delete this message for everyone?"
            : "Are you sure you want to delete this message for yourself only?";

        if (confirm(confirmText)) {
            $.post('/ContentCreator/Messages/DeleteMessage', { id: messageId, scope: deleteScope }, function () {
                messageElement.remove();
            });
        }
    });


    //Edit Message
    $(document).on('click', '.edit-message', function (e) {
        e.preventDefault();
        const messageId = $(this).data('id');
        const messageElement = $(this).closest('.message');
        const originalContent = messageElement.find('p').text();

        $('#editMessageInput').val(originalContent);
        editingMessageId = messageId;

        const modal = new bootstrap.Modal(document.getElementById('editMessageModal'));
        modal.show();
    });

    $(document).on('click', '#saveEditBtn', function () {
        const newContent = $('#editMessageInput').val();

        if (!newContent.trim()) {
            alert('The message cannot be empty.');
            return;
        }

        $.post('/ContentCreator/Messages/EditMessage', {
            id: editingMessageId,
            content: newContent
        }, function () {

            const messageElement = $(`.message[data-id="${editingMessageId}"]`);
            messageElement.find('p').text(newContent);


            const modal = bootstrap.Modal.getInstance(document.getElementById('editMessageModal'));
            modal.hide();


            editingMessageId = null;
        });
    });
    //Info
    $(document).on('click', '.info-message', function (e) {
        e.preventDefault();
        const messageId = $(this).data('id');

        $.get('/ContentCreator/Messages/MessageInfo', { id: messageId }, function (data) {
            $('#infoContent').html(
                `<strong>Sent:</strong> ${data.sentAt}<br>
                     <strong>State:</strong> ${data.isRead ? `✔️ Read<br><strong>Read At:</strong> ${data.readAt}` : '📭 Unread'}`
            );

            const box = $('#messageInfoBox');
            box.stop(true, true).fadeIn(300);

            setTimeout(() => {
                box.fadeOut(300);
            }, 3000);
        });
    });

    //Close Info
    $('#closeInfoBox').on('click', function () {
        $('#messageInfoBox').fadeOut(300);
    });
    let selectMode = false;
    const toggleSelectModeBtn = document.getElementById('toggleSelectModeBtn');
    const bulkActions = document.getElementById('bulkActions');
    const messagesContainer = document.getElementById('messagesContainer');
    const reaction = document.querySelector('.reaction-bar');

    //Select Button
    toggleSelectModeBtn.addEventListener('click', () => {
        selectMode = !selectMode;
        if (selectMode) {
            document.body.classList.add('select-mode');
            bulkActions.style.display = 'block';
            toggleSelectModeBtn.style.display = 'none';
            reaction.style.display = 'none';
            document.querySelectorAll('.message').forEach(msg => {
                const checkbox = msg.querySelector('.message-select-checkbox');
                if (checkbox) checkbox.style.display = 'inline-block';


                const dotsBtn = msg.querySelector('.dots-btn');
                if (dotsBtn) dotsBtn.style.display = 'none';
            });
            document.getElementById('cancelSelectBtn').style.display = 'inline-block';

        } else {
            cancelSelection();
        }
    });

    //Cancel Selection
    function cancelSelection() {
        selectMode = false;
        document.body.classList.remove('select-mode');
        bulkActions.style.display = 'none';
        toggleSelectModeBtn.style.display = 'inline-block';

        document.querySelectorAll('.message').forEach(msg => {
            const checkbox = msg.querySelector('.message-select-checkbox');
            if (checkbox) {
                checkbox.checked = false;
                checkbox.style.display = 'none';
            }
            msg.classList.remove('selected');
            const dotsBtn = msg.querySelector('.dots-btn');
            if (dotsBtn) dotsBtn.style.display = 'inline-block';
        });


        document.getElementById('cancelSelectBtn').style.display = 'none';
    }


    document.getElementById('cancelSelectBtn').addEventListener('click', cancelSelection);
    //Message CheckBox
    messagesContainer.addEventListener('change', (e) => {
        if (e.target.classList.contains('message-select-checkbox')) {
            const msgDiv = e.target.closest('.message');
            if (e.target.checked) {
                msgDiv.classList.add('selected');
            } else {
                msgDiv.classList.remove('selected');
            }
            updateBulkActionsUI();
        }
    });


    //Delete Selected For Me
    document.getElementById('deleteSelectedMe').addEventListener('click', () => {
        const selectedIds = Array.from(document.querySelectorAll('.message-select-checkbox:checked'))
            .map(cb => cb.closest('.message').getAttribute('data-id'));

        if (selectedIds.length === 0) return;

        if (confirm("Are you sure you want to delete selected messages for you?")) {
            $.ajax({
                url: '/ContentCreator/Messages/DeleteMultipleMessagesForMe',
                type: 'POST',
                contentType: 'application/json',
                data: JSON.stringify(selectedIds),
                success: () => {
                    selectedIds.forEach(id => document.querySelector(`.message[data-id="${id}"]`).remove());
                    cancelSelection();
                }
            });
        }
    });

    //Delete Selected For All
    document.getElementById('deleteSelectedAll').addEventListener('click', () => {
        const selectedIds = Array.from(document.querySelectorAll('.message-select-checkbox:checked'))
            .map(cb => cb.closest('.message').getAttribute('data-id'));

        if (selectedIds.length === 0) return;

        if (confirm("Are you sure you want to delete selected messages for everyone?")) {
            $.ajax({
                url: '/ContentCreator/Messages/DeleteMultipleMessagesForAll',
                type: 'POST',
                contentType: 'application/json',
                data: JSON.stringify(selectedIds),
                success: () => {
                    selectedIds.forEach(id => document.querySelector(`.message[data-id="${id}"]`).remove());
                    cancelSelection();
                }
            });
        }
    });
    //Update Bulk Actions
    function updateBulkActionsUI() {
        const currentUserId = document.getElementById('senderId').value;
        const selectedMessages = Array.from(document.querySelectorAll('.message-select-checkbox:checked'))
            .map(cb => cb.closest('.message'));

        const bulkDeleteAll = document.getElementById('bulkDeleteAll');
        const cancelSelectBtn = document.getElementById('cancelSelectBtn');

        if (selectedMessages.length === 0) {
            bulkDeleteAll.style.display = 'none';
            cancelSelectBtn.style.display = 'none';
            return;
        }

        let allFromMe = selectedMessages.every(msg => msg.dataset.senderId === currentUserId);

        bulkDeleteAll.style.display = allFromMe ? 'block' : 'none';
        cancelSelectBtn.style.display = 'inline-block';
    }
    //Bulk Delete for me
    document.getElementById('bulkDeleteMe').addEventListener('click', () => {
        document.getElementById('deleteSelectedMe').click();
    });
    //Bulk Delete All
    document.getElementById('bulkDeleteAll').addEventListener('click', () => {
        document.getElementById('deleteSelectedAll').click();
    });

    //Bulk Copy
    document.getElementById('bulkCopy').addEventListener('click', () => {
        const selectedTexts = Array.from(document.querySelectorAll('.message-select-checkbox:checked'))
            .map(cb => cb.closest('.message').querySelector('p')?.innerText || '')
            .join('\n\n');

        if (selectedTexts) {
            navigator.clipboard.writeText(selectedTexts).then(() => {
                alert("Messages copied to clipboard!");
            });
        }
    });

    //Copy Message
    document.addEventListener('click', async (e) => {
        const target = e.target;
        if (!target.classList.contains('copy-message')) return;

        e.preventDefault();

        const messageId = target.getAttribute('data-id');
        const messageElement = document.querySelector(`.message[data-id="${messageId}"] p`);

        if (messageElement) {
            const textToCopy = messageElement.textContent;
            await navigator.clipboard.writeText(textToCopy);
            alert('Message copied to clipboard!');
        }
    });

    //Pin Message
    document.addEventListener('click', async (e) => {
        const target = e.target;
        if (!target.classList.contains('pin-message')) return;

        e.preventDefault();

        const messageId = target.getAttribute('data-id');
        const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
        if (!tokenInput) {
            console.error('Anti forgery token not found!');
            return;
        }
        const token = tokenInput.value;

        const response = await fetch('/ContentCreator/Messages/TogglePinMessage', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token
            },
            body: JSON.stringify({ messageId: parseInt(messageId) })
        });

        if (!response.ok) {
            alert('Server error!');
            return;
        }

        const result = await response.json();
        if (result.success) {
            updatePinnedMessageContainer(result.message);

            if (result.message && result.message.id == messageId) {
                target.textContent = "Unpin";
            } else {
                target.textContent = "Pin";
            }
        }



    });


    function updatePinnedMessageContainer(pinnedMessage) {
        const container = document.getElementById('pinnedMessageContainer');

        if (!pinnedMessage || !pinnedMessage.isPinned) {

            container.style.display = 'none';
            container.innerHTML = '';
            return;
        }

        container.style.display = 'block';
        container.innerHTML = `
        <strong>Pinned:</strong>
        <span>${escapeHtml(pinnedMessage.content)}</span>
        <button class="btn btn-sm btn-outline-dark float-end pin-message" data-id="${pinnedMessage.id}">
            Unpin
        </button>
    `;
    }

    function escapeHtml(text) {
        return text.replace(/[&<>"']/g, function (m) {
            return ({
                '&': '&amp;',
                '<': '&lt;',
                '>': '&gt;',
                '"': '&quot;',
                "'": '&#39;'
            })[m];
        });
    }

    //Message Reaction

    document.addEventListener("click", function (e) {
        if (e.target.classList.contains("emoji-option")) {
            const emoji = e.target.textContent.trim();
            const messageDiv = e.target.closest(".message");
            const messageId = parseInt(messageDiv.getAttribute("data-id"));
            const senderId = document.getElementById("senderId").value;

            connection.invoke("SendReaction", senderId, messageId, emoji)
                .catch(err => console.error("Error sending reaction:", err));
        }
    });
    connection.on("ReceiveReactionUpdate", function (messageId) {
        const messageElement = document.querySelector(`.message[data-id='${messageId}']`);
        if (!messageElement) return;

      
        fetch(`/ContentCreator/Messages/GetReactionsForMessage/${messageId}`)
            .then(response => response.json())
            .then(data => {
                let summaryDiv = messageElement.querySelector(".reaction-summary");

                if (!summaryDiv) {
                    summaryDiv = document.createElement("div");
                    summaryDiv.className = "reaction-summary";
                    messageElement.appendChild(summaryDiv);
                }

                summaryDiv.innerHTML = "";  

                const reactionGroups = {};
                data.forEach(r => {
                    if (!reactionGroups[r.reaction]) reactionGroups[r.reaction] = 0;
                    reactionGroups[r.reaction]++;
                });

                for (let emoji in reactionGroups) {
                    const span = document.createElement("span");
                    span.textContent = `${emoji} ${reactionGroups[emoji]}`;
                    span.className = "reaction-count";

                    summaryDiv.appendChild(span);
                }
            })
            .catch(err => console.error("Error fetching reactions (update):", err));
    });


    //Export
    document.getElementById('bulkExport')?.addEventListener('click', function (e) {
        e.preventDefault();

        const selectedMessages = [...document.querySelectorAll('.message-select-checkbox:checked')]
            .map(checkbox => {
                const messageElement = checkbox.closest('.message');

                if (!messageElement) return null;

                const senderId = messageElement.getAttribute('data-sender-id') || "Unknown";
                const messageText = messageElement.querySelector('p')?.innerText.trim() || "";
                const timeText = messageElement.querySelector('small')?.innerText.trim() || "";

                return `Sender ID: ${senderId}\nTime: ${timeText}\nMessage: ${messageText}`;
            })
            .filter(msg => msg);

        if (selectedMessages.length === 0) {
            alert('No messages selected.');
            return;
        }

        const content = selectedMessages.join('\n\n-------------------------\n\n');

        const blob = new Blob([content], { type: 'text/plain' });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = 'chat-export.txt';
        a.click();
        URL.revokeObjectURL(url);
    });

    //Clear Chat
  
    //document.addEventListener('click', async function (e) {
    //    if (e.target && e.target.id === "clearChatBtn") {
    //        e.preventDefault();

    //        const senderId = document.getElementById("senderId")?.value;
    //        const receiverId = document.getElementById("receiverId")?.value;
    //        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

    //        if (!senderId || !receiverId || !token) {
    //            return alert("⛔ بيانات المستخدم غير موجودة");
    //        }

    //        const confirmed = confirm("هل تريد مسح جميع الرسائل من طرفك؟");
    //        if (!confirmed) return;

    //        try {
    //            const response = await fetch(`/ContentCreator/Messages/ClearChat?userId=${receiverId}`, {
    //                method: "POST",
    //                headers: {
    //                    "RequestVerificationToken": token
    //                }
    //            });

    //            if (response.ok) {
    //                location.reload();
    //            } else {
    //                alert("فشلت عملية الحذف، تأكد أن كل شيء مضبوط");
    //            }
    //        } catch (err) {
    //            console.error(err);
    //            alert("حدث خطأ أثناء الحذف");
    //        }
    //    }
    //});


   
    const messageArea = document.querySelector('.chat-messages');
    const colorPicker = document.getElementById('bgColorPicker');
    const suggestedImages = document.querySelectorAll('#suggestedImages img');
    const bgImageUpload = document.getElementById('bgImageUpload');
    const resetBtn = document.getElementById('resetBg');

    // Load saved background
    const savedBg = localStorage.getItem('chat-bg');
    const savedIsImage = localStorage.getItem('chat-bg-isImage') === 'true';

    if (savedBg) {
        if (savedIsImage) {
            messageArea.style.backgroundImage = `url(${savedBg})`;
            messageArea.style.backgroundColor = 'transparent';
        } else {
            messageArea.style.setProperty('--chat-bg', savedBg);
            colorPicker.value = savedBg;
            messageArea.style.backgroundImage = 'none';
        }
    }

    // Color Picker
    colorPicker.addEventListener('input', function () {
        const selectedColor = this.value;
        messageArea.style.backgroundColor = selectedColor;
        messageArea.style.backgroundImage = 'none';
        localStorage.setItem('chat-bg', selectedColor);
        localStorage.setItem('chat-bg-isImage', false);
    });

    // Suggested Images
    suggestedImages.forEach(img => {
        img.addEventListener('click', () => {
            const src = img.src;
            messageArea.style.backgroundImage = `url(${src})`;
            messageArea.style.backgroundColor = 'transparent';
            localStorage.setItem('chat-bg', src);
            localStorage.setItem('chat-bg-isImage', true);
        });
    });

    // Upload Image
    bgImageUpload.addEventListener('change', (e) => {
        const file = e.target.files[0];
        if (!file) return;
        const reader = new FileReader();
        reader.onload = function (event) {
            messageArea.style.backgroundImage = `url(${event.target.result})`;
            messageArea.style.backgroundColor = 'transparent';
            localStorage.setItem('chat-bg', event.target.result);
            localStorage.setItem('chat-bg-isImage', true);
        }
        reader.readAsDataURL(file);
    });

    // Reset to default
    resetBtn.addEventListener('click', () => {
        messageArea.style.backgroundImage = 'none';
        messageArea.style.backgroundColor = '#fff';
        colorPicker.value = '#ffffff';
        localStorage.removeItem('chat-bg');
        localStorage.removeItem('chat-bg-isImage');
    });

    //search in chat
    const searchBtn = document.getElementById('bulkSearch');
    const searchBar = document.getElementById('chatSearchBar');
    const searchInput = document.getElementById('chatSearchInput');
     const closeBtn = document.getElementById('closeSearch');
    const searchCount = document.getElementById('searchCount');

    searchBtn.addEventListener('click', (e) => {
        e.preventDefault();
        searchBar.classList.toggle('d-none');
        if (!searchBar.classList.contains('d-none')) {
            searchInput.focus();
        } else {
            clearHighlights();
            searchCount.textContent = '';
        }
    });

    searchInput.addEventListener('input', () => {
        const query = searchInput.value.toLowerCase();
        const messages = messagesContainer.querySelectorAll('.message');
        let matchCount = 0;
        let firstMatch = null;

        messages.forEach(msg => {
            const p = msg.querySelector('p');
            const text = p.textContent;
            p.innerHTML = text;  

            if (query) {
                const regex = new RegExp(`(${query})`, 'gi');
                if (regex.test(text)) {
                    p.innerHTML = text.replace(regex, '<span class="highlight">$1</span>');
                    matchCount++;
                    if (!firstMatch) firstMatch = msg;
                }
            }
        });

        searchCount.textContent = matchCount > 0 ? `${matchCount} result(s)` : '';
 
        if (firstMatch) {
            firstMatch.scrollIntoView({ behavior: 'smooth', block: 'center' });
        }
    });

    closeBtn.addEventListener('click', () => {
        searchBar.classList.add('d-none');
        searchInput.value = '';
        searchCount.textContent = '';
        clearHighlights();
    });

    function clearHighlights() {
        const messages = messagesContainer.querySelectorAll('.message');
        messages.forEach(msg => {
            const p = msg.querySelector('p');
            p.innerHTML = p.textContent;
        });
    }


    //emoji library
    import('https://cdn.jsdelivr.net/npm/emoji-picker-element@1.8.0/index.js').then(() => {
        const picker = document.createElement('emoji-picker');
        picker.style.position = 'absolute';
        picker.style.bottom = '60px';
        picker.style.right = '20px';
        picker.style.zIndex = '1000';
        picker.style.display = 'none';
        document.body.appendChild(picker);

        const input = document.querySelector('#messageInput');
        const emojiBtn = document.querySelector('#emojiBtn');

         emojiBtn.addEventListener('click', () => {
            picker.style.display = picker.style.display === 'none' ? 'block' : 'none';
        });

        let currentMessageDiv = null;

        document.addEventListener('click', function (e) {
            const target = e.target;

             if (target.classList.contains('emoji-plus')) {
                const rect = target.getBoundingClientRect();
                picker.style.top = `${rect.top - picker.offsetHeight}px`;
                picker.style.left = `${rect.left}px`;
                picker.style.display = 'block';
                e.stopPropagation();

                currentMessageDiv = target.closest('.message');
            } else if (!picker.contains(target) && target !== emojiBtn) {
                picker.style.display = 'none';
                currentMessageDiv = null;
            }
        });

        picker.addEventListener('emoji-click', event => {
            const emoji = event.detail.unicode;

            if (currentMessageDiv) {
                const messageId = parseInt(currentMessageDiv.getAttribute('data-id'));
                const senderId = document.getElementById("senderId").value;

                if (typeof connection !== "undefined") {
                    connection.invoke("SendReaction", senderId, messageId, emoji)
                        .catch(err => console.error("Error sending reaction:", err));
                }

                picker.style.display = 'none';
                currentMessageDiv = null;
            } else {
                 const start = input.selectionStart;
                const end = input.selectionEnd;
                input.value = input.value.slice(0, start) + emoji + input.value.slice(end);
                input.focus();
                input.selectionStart = input.selectionEnd = start + emoji.length;
                picker.style.display = 'none';
            }
        });
    });
});
