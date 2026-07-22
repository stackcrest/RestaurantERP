using RestaurantERP.Domain.Enums;

namespace RestaurantERP.Application.Common;

public static class AppContentSections
{
    public const string Site = "Site";
    public const string Seo = "SEO";
    public const string Home = "Home";
    public const string Footer = "Footer";
    public const string About = "About";
    public const string Contact = "Contact";
    public const string Menu = "Menu";
    public const string Blog = "Blog";
    public const string Auth = "Auth";
    public const string Social = "Social";
    public const string Integrations = "Integrations";
    public const string Placeholders = "Placeholders";

    public static readonly string[] All =
    [
        Site, Seo, Home, Footer, About, Contact, Menu, Blog, Auth, Social, Integrations, Placeholders
    ];
}

public record AppContentSeed(
    string Key,
    string Value,
    AppContentType Type,
    string Section,
    string Label,
    string? Description = null,
    int SortOrder = 0);

public static class AppContentDefaults
{
    public static IEnumerable<AppContentSeed> GetAll() =>
    [
        // Site
        new("site.name", "Restaurant ERP", AppContentType.Text, AppContentSections.Site, "Site Name", "Brand name shown in navbar and footer", 1),
        new("site.tagline", "Delicious food, delivered fast", AppContentType.Text, AppContentSections.Site, "Site Tagline", null, 2),
        new("site.logo_icon", "bi-fire", AppContentType.Text, AppContentSections.Site, "Logo Icon Class", "Bootstrap icon class e.g. bi-fire", 3),

        // SEO
        new("seo.default_description", "Restaurant ERP - Delicious food, delivered fast", AppContentType.Text, AppContentSections.Seo, "Default Meta Description", null, 1),
        new("seo.title_suffix", "Restaurant ERP", AppContentType.Text, AppContentSections.Seo, "Page Title Suffix", "Appended after page title", 2),

        // Home
        new("home.hero.title", "Delicious Food,<br>Delivered To You", AppContentType.Html, AppContentSections.Home, "Hero Title", null, 1),
        new("home.hero.subtitle", "Experience the finest cuisines crafted with passion. Order online for dine-in, takeaway, or home delivery.", AppContentType.Text, AppContentSections.Home, "Hero Subtitle", null, 2),
        new("home.hero.cta_primary", "Browse Menu", AppContentType.Text, AppContentSections.Home, "Hero Primary Button", null, 3),
        new("home.hero.cta_primary_url", "/Menu", AppContentType.Url, AppContentSections.Home, "Hero Primary URL", null, 4),
        new("home.hero.cta_secondary", "Learn More", AppContentType.Text, AppContentSections.Home, "Hero Secondary Button", null, 5),
        new("home.hero.cta_secondary_url", "/Home/About", AppContentType.Url, AppContentSections.Home, "Hero Secondary URL", null, 6),
        new("home.hero.image", "https://images.unsplash.com/photo-1504674900247-0877df9cc836?w=600", AppContentType.Image, AppContentSections.Home, "Hero Image", null, 7),
        new("home.features.1.title", "Fast Delivery", AppContentType.Text, AppContentSections.Home, "Feature 1 Title", null, 10),
        new("home.features.1.body", "Get your food delivered in 30 minutes or less", AppContentType.Text, AppContentSections.Home, "Feature 1 Description", null, 11),
        new("home.features.1.icon", "bi-clock", AppContentType.Text, AppContentSections.Home, "Feature 1 Icon", "Bootstrap icon class", 12),
        new("home.features.2.title", "Quality Food", AppContentType.Text, AppContentSections.Home, "Feature 2 Title", null, 13),
        new("home.features.2.body", "Fresh ingredients and authentic recipes", AppContentType.Text, AppContentSections.Home, "Feature 2 Description", null, 14),
        new("home.features.2.icon", "bi-award", AppContentType.Text, AppContentSections.Home, "Feature 2 Icon", null, 15),
        new("home.features.3.title", "Safe & Hygienic", AppContentType.Text, AppContentSections.Home, "Feature 3 Title", null, 16),
        new("home.features.3.body", "Prepared with highest hygiene standards", AppContentType.Text, AppContentSections.Home, "Feature 3 Description", null, 17),
        new("home.features.3.icon", "bi-shield-check", AppContentType.Text, AppContentSections.Home, "Feature 3 Icon", null, 18),
        new("home.categories.title", "Explore Categories", AppContentType.Text, AppContentSections.Home, "Categories Title", null, 20),
        new("home.categories.subtitle", "Find your favorite cuisine", AppContentType.Text, AppContentSections.Home, "Categories Subtitle", null, 21),
        new("home.featured.title", "Featured Dishes", AppContentType.Text, AppContentSections.Home, "Featured Title", null, 22),
        new("home.featured.subtitle", "Our chef's special recommendations", AppContentType.Text, AppContentSections.Home, "Featured Subtitle", null, 23),
        new("home.cta.title", "Ready to Order?", AppContentType.Text, AppContentSections.Home, "CTA Title", null, 24),
        new("home.cta.subtitle", "Sign up now and get 10% off on your first order!", AppContentType.Text, AppContentSections.Home, "CTA Subtitle", null, 25),

        // Footer
        new("footer.tagline", "Delicious food made with love. Order online for dine-in, takeaway, or delivery.", AppContentType.Text, AppContentSections.Footer, "Footer Tagline", null, 1),
        new("footer.hours.weekday", "Mon - Fri: 10:00 AM - 10:00 PM", AppContentType.Text, AppContentSections.Footer, "Weekday Hours", null, 2),
        new("footer.hours.saturday", "Saturday: 10:00 AM - 11:00 PM", AppContentType.Text, AppContentSections.Footer, "Saturday Hours", null, 3),
        new("footer.hours.sunday", "Sunday: 11:00 AM - 9:00 PM", AppContentType.Text, AppContentSections.Footer, "Sunday Hours", null, 4),
        new("footer.address", "123 Food Street, City", AppContentType.Text, AppContentSections.Footer, "Footer Address", null, 5),
        new("footer.phone", "+91 98765 43210", AppContentType.Text, AppContentSections.Footer, "Footer Phone", null, 6),
        new("footer.email", "info@restaurant.com", AppContentType.Text, AppContentSections.Footer, "Footer Email", null, 7),
        new("footer.copyright", "All rights reserved.", AppContentType.Text, AppContentSections.Footer, "Copyright Text", "Shown after year and site name", 8),

        // About
        new("about.story.title", "Our Story", AppContentType.Text, AppContentSections.About, "Story Title", null, 1),
        new("about.story.lead", "A passion for food that started over 20 years ago.", AppContentType.Text, AppContentSections.About, "Story Lead", null, 2),
        new("about.story.body1", "Restaurant ERP was founded with a simple mission: to bring people together through great food. What started as a small family kitchen has grown into a beloved dining destination, serving thousands of happy customers every day.", AppContentType.Text, AppContentSections.About, "Story Paragraph 1", null, 3),
        new("about.story.body2", "Our chefs combine traditional recipes with modern techniques to create dishes that are both familiar and exciting. We source only the freshest ingredients from local suppliers, ensuring that every bite is packed with flavor and nutrition.", AppContentType.Text, AppContentSections.About, "Story Paragraph 2", null, 4),
        new("about.story.image", "https://images.unsplash.com/photo-1414235077428-338989a2e8c0?w=600", AppContentType.Image, AppContentSections.About, "Story Image", null, 5),
        new("about.values.title", "Our Values", AppContentType.Text, AppContentSections.About, "Values Title", null, 10),
        new("about.values.subtitle", "What drives us every day", AppContentType.Text, AppContentSections.About, "Values Subtitle", null, 11),
        new("about.values.1.title", "Passion", AppContentType.Text, AppContentSections.About, "Value 1 Title", null, 12),
        new("about.values.1.body", "We pour our heart into every dish we create, treating each meal as a work of art.", AppContentType.Text, AppContentSections.About, "Value 1 Description", null, 13),
        new("about.values.1.icon", "bi-heart", AppContentType.Text, AppContentSections.About, "Value 1 Icon", null, 14),
        new("about.values.2.title", "Freshness", AppContentType.Text, AppContentSections.About, "Value 2 Title", null, 15),
        new("about.values.2.body", "Only the finest, freshest ingredients make it to our kitchen and onto your plate.", AppContentType.Text, AppContentSections.About, "Value 2 Description", null, 16),
        new("about.values.2.icon", "bi-leaf", AppContentType.Text, AppContentSections.About, "Value 2 Icon", null, 17),
        new("about.values.3.title", "Community", AppContentType.Text, AppContentSections.About, "Value 3 Title", null, 18),
        new("about.values.3.body", "We believe in bringing people together and creating memorable dining experiences.", AppContentType.Text, AppContentSections.About, "Value 3 Description", null, 19),
        new("about.values.3.icon", "bi-people", AppContentType.Text, AppContentSections.About, "Value 3 Icon", null, 20),
        new("about.team.title", "Meet Our Team", AppContentType.Text, AppContentSections.About, "Team Title", null, 21),
        new("about.team.subtitle", "The people behind the magic", AppContentType.Text, AppContentSections.About, "Team Subtitle", null, 22),
        new("about.team.1.name", "Chef Rajesh", AppContentType.Text, AppContentSections.About, "Team Member 1 Name", null, 23),
        new("about.team.1.role", "Head Chef", AppContentType.Text, AppContentSections.About, "Team Member 1 Role", null, 24),
        new("about.team.1.image", "https://images.unsplash.com/photo-1577219491135-ce391730fb2c?w=200&h=200&fit=crop", AppContentType.Image, AppContentSections.About, "Team Member 1 Photo", null, 25),
        new("about.team.2.name", "Priya Sharma", AppContentType.Text, AppContentSections.About, "Team Member 2 Name", null, 26),
        new("about.team.2.role", "Pastry Chef", AppContentType.Text, AppContentSections.About, "Team Member 2 Role", null, 27),
        new("about.team.2.image", "https://images.unsplash.com/photo-1583394293214-28e1dbb71a1e?w=200&h=200&fit=crop", AppContentType.Image, AppContentSections.About, "Team Member 2 Photo", null, 28),
        new("about.team.3.name", "Amit Verma", AppContentType.Text, AppContentSections.About, "Team Member 3 Name", null, 29),
        new("about.team.3.role", "Restaurant Manager", AppContentType.Text, AppContentSections.About, "Team Member 3 Role", null, 30),
        new("about.team.3.image", "https://images.unsplash.com/photo-1560250097-0b93528c311a?w=200&h=200&fit=crop", AppContentType.Image, AppContentSections.About, "Team Member 3 Photo", null, 31),
        new("about.stats.years", "20+", AppContentType.Text, AppContentSections.About, "Stat: Years", null, 32),
        new("about.stats.years_label", "Years of Excellence", AppContentType.Text, AppContentSections.About, "Stat: Years Label", null, 33),
        new("about.stats.customers", "50K+", AppContentType.Text, AppContentSections.About, "Stat: Customers", null, 34),
        new("about.stats.customers_label", "Happy Customers", AppContentType.Text, AppContentSections.About, "Stat: Customers Label", null, 35),
        new("about.stats.items", "100+", AppContentType.Text, AppContentSections.About, "Stat: Menu Items", null, 36),
        new("about.stats.items_label", "Menu Items", AppContentType.Text, AppContentSections.About, "Stat: Menu Items Label", null, 37),
        new("about.stats.chefs", "15+", AppContentType.Text, AppContentSections.About, "Stat: Chefs", null, 38),
        new("about.stats.chefs_label", "Expert Chefs", AppContentType.Text, AppContentSections.About, "Stat: Chefs Label", null, 39),

        // Contact
        new("contact.title", "Contact Us", AppContentType.Text, AppContentSections.Contact, "Page Title", null, 1),
        new("contact.subtitle", "We'd love to hear from you", AppContentType.Text, AppContentSections.Contact, "Page Subtitle", null, 2),
        new("contact.form.title", "Get In Touch", AppContentType.Text, AppContentSections.Contact, "Form Section Title", null, 3),
        new("contact.form.intro", "Have questions about our menu, want to make a reservation, or need to provide feedback? Fill out the form and we'll get back to you as soon as possible.", AppContentType.Text, AppContentSections.Contact, "Form Intro", null, 4),
        new("contact.address", "123 Food Street, Sector 15<br>New Delhi, 110001", AppContentType.Html, AppContentSections.Contact, "Address", null, 5),
        new("contact.phone", "+91 98765 43210", AppContentType.Text, AppContentSections.Contact, "Phone", null, 6),
        new("contact.phone2", "+91 98765 43211", AppContentType.Text, AppContentSections.Contact, "Secondary Phone", null, 7),
        new("contact.email", "info@restaurant.com", AppContentType.Text, AppContentSections.Contact, "Email", null, 8),
        new("contact.email2", "reservations@restaurant.com", AppContentType.Text, AppContentSections.Contact, "Reservations Email", null, 9),
        new("contact.hours.weekday", "Mon - Fri: 10:00 AM - 10:00 PM", AppContentType.Text, AppContentSections.Contact, "Weekday Hours", null, 10),
        new("contact.hours.weekend", "Sat - Sun: 11:00 AM - 11:00 PM", AppContentType.Text, AppContentSections.Contact, "Weekend Hours", null, 11),
        new("contact.map.embed_url", "https://www.google.com/maps/embed?pb=!1m18!1m12!1m3!1d3504.1234567890!2d77.2090!3d28.6139!2m3!1f0!2f0!3f0!3m2!1i1024!2i768!4f13.1!3m3!1m2!1s0x0%3A0x0!2zMjjCsDM2JzUwLjAiTiA3N8KwMTInMzIuNCJF!5e0!3m2!1sen!2sin!4v1234567890", AppContentType.Url, AppContentSections.Contact, "Google Maps Embed URL", "iframe src URL", 12),

        // Menu
        new("menu.hero.title", "Our Menu", AppContentType.Text, AppContentSections.Menu, "Menu Page Title", null, 1),
        new("menu.hero.subtitle", "Explore our delicious selection of dishes", AppContentType.Text, AppContentSections.Menu, "Menu Page Subtitle", null, 2),
        new("menu.item.fallback_image", "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?w=400", AppContentType.Image, AppContentSections.Menu, "Default Dish Image", "Used when menu item has no image", 3),

        // Blog
        new("blog.hero.title", "Our Blog", AppContentType.Text, AppContentSections.Blog, "Blog Page Title", null, 1),
        new("blog.hero.subtitle", "Stories, tips, and updates from our kitchen", AppContentType.Text, AppContentSections.Blog, "Blog Page Subtitle", null, 2),
        new("blog.post.fallback_image", "https://images.unsplash.com/photo-1504674900247-0877df9cc836?w=800", AppContentType.Image, AppContentSections.Blog, "Default Blog Image", null, 3),

        // Auth
        new("auth.login.title", "Welcome Back", AppContentType.Text, AppContentSections.Auth, "Login Title", null, 1),
        new("auth.login.subtitle", "Sign in to your account", AppContentType.Text, AppContentSections.Auth, "Login Subtitle", null, 2),
        new("auth.login.background", "https://images.unsplash.com/photo-1414235077428-338989a2e8c0?w=1200", AppContentType.Image, AppContentSections.Auth, "Login Background", null, 3),
        new("auth.register.title", "Create Account", AppContentType.Text, AppContentSections.Auth, "Register Title", null, 4),
        new("auth.register.subtitle", "Join us and start ordering delicious food", AppContentType.Text, AppContentSections.Auth, "Register Subtitle", null, 5),
        new("auth.register.background", "https://images.unsplash.com/photo-1504674900247-0877df9cc836?w=1200", AppContentType.Image, AppContentSections.Auth, "Register Background", null, 6),

        // Social
        new("social.facebook", "#", AppContentType.Url, AppContentSections.Social, "Facebook URL", null, 1),
        new("social.instagram", "#", AppContentType.Url, AppContentSections.Social, "Instagram URL", null, 2),
        new("social.twitter", "#", AppContentType.Url, AppContentSections.Social, "Twitter/X URL", null, 3),
        new("social.youtube", "#", AppContentType.Url, AppContentSections.Social, "YouTube URL", null, 4),

        // Integrations
        new("integrations.whatsapp.enabled", "true", AppContentType.Text, AppContentSections.Integrations, "WhatsApp Enabled", "Set to true to show WhatsApp for logged-in customers", 1),
        new("integrations.whatsapp.number", "919876543210", AppContentType.Text, AppContentSections.Integrations, "WhatsApp Number", "Country code + number, digits only e.g. 919876543210", 2),
        new("integrations.whatsapp.greeting", "Hi! I'm {name}. I need help with my order at {siteName}.", AppContentType.Text, AppContentSections.Integrations, "Support Message Template", "Placeholders: {name}, {email}, {siteName}", 3),
        new("integrations.whatsapp.order_message", "Hi, I'm {name} ({email}). I have a question about my order #{orderNumber}.", AppContentType.Text, AppContentSections.Integrations, "Order Message Template", "Placeholders: {name}, {email}, {orderNumber}, {siteName}", 4),
        new("integrations.whatsapp.button_label", "Chat on WhatsApp", AppContentType.Text, AppContentSections.Integrations, "Button Label", "Floating button tooltip/label", 5),

        // Placeholders — editable form input hints
        new("placeholder.menu_search", "Search dishes...", AppContentType.Text, AppContentSections.Placeholders, "Menu Search", "Search box on public menu page", 1),
        new("placeholder.email", "you@example.com", AppContentType.Text, AppContentSections.Placeholders, "Email", "Login / register / contact email fields", 2),
        new("placeholder.password", "••••••••", AppContentType.Text, AppContentSections.Placeholders, "Password", "Password input hint", 3),
        new("placeholder.full_name", "John Doe", AppContentType.Text, AppContentSections.Placeholders, "Full Name", null, 4),
        new("placeholder.phone", "+91 98765 43210", AppContentType.Text, AppContentSections.Placeholders, "Phone", null, 5),
        new("placeholder.address", "House no., building, street", AppContentType.Text, AppContentSections.Placeholders, "Address", "Checkout address field", 6),
        new("placeholder.landmark", "Near metro, mall, etc.", AppContentType.Text, AppContentSections.Placeholders, "Landmark", null, 7),
        new("placeholder.city", "City", AppContentType.Text, AppContentSections.Placeholders, "City", null, 8),
        new("placeholder.pincode", "110001", AppContentType.Text, AppContentSections.Placeholders, "PIN Code", null, 9),
        new("placeholder.order_notes", "Any special requests?", AppContentType.Text, AppContentSections.Placeholders, "Order Notes", null, 10),
        new("placeholder.contact_message", "Tell us how we can help...", AppContentType.Text, AppContentSections.Placeholders, "Contact Message", null, 11),
        new("placeholder.image_url", "https://...", AppContentType.Text, AppContentSections.Placeholders, "Image URL", "Fallback URL when file upload is not used", 12),
        new("placeholder.review_comment", "Share your experience...", AppContentType.Text, AppContentSections.Placeholders, "Review Comment", null, 13),
    ];
}
