let postUrl = ''; // To store the post URL

// Show the share dialog and set the post URL
function showShareDialog(url) {
    postUrl = url;
    var myModal = new bootstrap.Modal(document.getElementById('shareModal'));
    myModal.show();
}

// Copy the link to clipboard
function copyLink() {
    navigator.clipboard.writeText(postUrl).then(() => {
        alert('Link copied!');
    });
}

// Share on WhatsApp
function shareOnWhatsApp() {
    const whatsappUrl = `https://wa.me/?text=${encodeURIComponent(postUrl)}`;
    window.open(whatsappUrl, '_blank');
}

// Share on Telegram
function shareOnTelegram() {
    const telegramUrl = `https://t.me/share/url?url=${encodeURIComponent(postUrl)}`;
    window.open(telegramUrl, '_blank');
}
