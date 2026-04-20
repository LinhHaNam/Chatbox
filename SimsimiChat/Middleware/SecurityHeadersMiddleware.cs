// Security configuration and best practices for the application

namespace SimsimiChat.Middleware;

/// <summary>
/// Middleware to add security headers to all HTTP responses
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Prevent clickjacking attacks
        context.Response.Headers["X-Frame-Options"] = "DENY";
        
        // Prevent MIME sniffing
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        
        // Enable XSS filtering in browsers
        context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
        
        // Content Security Policy
        context.Response.Headers["Content-Security-Policy"] = "default-src 'self'";
        
        // Referrer Policy
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        
        // Permissions Policy
        context.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";

        await _next(context);
    }
}
