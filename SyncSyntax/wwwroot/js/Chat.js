
document.addEventListener('DOMContentLoaded', function () {
 
var connection = new signalR.HubConnectionBuilder()
    .withUrl("/chathub")
    .build();


    connection.start()
        .then(() => {
            console.log("SignalR connected");

        })
        .catch(err => console.error("SignalR connection failed:", err.toString()));



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

    <div class="reaction-summary">
      ${reactions && reactions.length > 0
                    ? reactions.map(r => `<span class="reaction-count">${r.reaction} ${r.count}</span>`).join('')
                    : ''}
    </div>

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
            bootstrap.Dropdown.getOrCreateInstance(messageElement.querySelector('[data-bs-toggle="dropdown"]'));
            messageContainer.scrollTop = messageContainer.scrollHeight;

        } catch (error) {
            console.error("Error appending message:", error);
        }
    });


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


$('#closeInfoBox').on('click', function () {
    $('#messageInfoBox').fadeOut(300);
});
let selectMode = false;
const toggleSelectModeBtn = document.getElementById('toggleSelectModeBtn');
const bulkActions = document.getElementById('bulkActions');
const messagesContainer = document.getElementById('messagesContainer');
 const reaction = document.querySelector('.reaction-bar');


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

document.getElementById('bulkDeleteMe').addEventListener('click', () => {
    document.getElementById('deleteSelectedMe').click();
});

document.getElementById('bulkDeleteAll').addEventListener('click', () => {
    document.getElementById('deleteSelectedAll').click();
});

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
    connection.on("ReceiveReactionUpdate", function (messageId, updatedReactions) {
        const messageElement = document.querySelector(`.message[data-id='${messageId}']`);
        if (!messageElement) return;

        
        let existingReactionSummary = messageElement.querySelector(".reaction-summary");
        if (existingReactionSummary) {
            existingReactionSummary.remove();
        }

      
        const summaryDiv = document.createElement("div");
        summaryDiv.className = "reaction-summary";

        updatedReactions.forEach(r => {
            const span = document.createElement("span");
            span.textContent = `${r.reaction} ${r.count}`;
            span.className = "reaction-count";
            summaryDiv.appendChild(span);
        });

        messageElement.appendChild(summaryDiv);
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
});
