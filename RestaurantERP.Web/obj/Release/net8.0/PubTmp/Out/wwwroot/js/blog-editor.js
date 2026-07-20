document.addEventListener('DOMContentLoaded', function () {
    const contentEl = document.getElementById('blogContent');
    if (contentEl && typeof $ !== 'undefined' && $.fn.summernote) {
        $('#blogContent').summernote({
            height: 420,
            placeholder: 'Write your blog post here...',
            toolbar: [
                ['style', ['style']],
                ['font', ['bold', 'italic', 'underline', 'clear']],
                ['fontname', ['fontname']],
                ['color', ['color']],
                ['para', ['ul', 'ol', 'paragraph']],
                ['table', ['table']],
                ['insert', ['link', 'picture', 'video', 'hr']],
                ['view', ['fullscreen', 'codeview', 'help']]
            ]
        });
    }

    const titleInput = document.getElementById('blogTitle');
    const slugInput = document.getElementById('blogSlug');
    if (titleInput && slugInput && !slugInput.value) {
        titleInput.addEventListener('blur', function () {
            if (!slugInput.value.trim()) {
                slugInput.value = titleInput.value
                    .toLowerCase()
                    .replace(/[^a-z0-9\s-]/g, '')
                    .trim()
                    .replace(/\s+/g, '-')
                    .replace(/-+/g, '-');
            }
        });
    }

    const urlInput = document.getElementById('featuredImageUrl');
    const fileInput = document.getElementById('featuredImage');
    const preview = document.getElementById('imagePreview');

    function showPreview(src) {
        if (!preview || !src) return;
        preview.src = src;
        preview.classList.remove('d-none');
    }

    if (urlInput) {
        urlInput.addEventListener('input', function () {
            if (urlInput.value) showPreview(urlInput.value);
        });
    }

    if (fileInput) {
        fileInput.addEventListener('change', function () {
            const file = fileInput.files?.[0];
            if (file) showPreview(URL.createObjectURL(file));
        });
    }
});
