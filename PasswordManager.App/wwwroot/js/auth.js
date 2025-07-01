// Auth.js - Enhance 1Password login experience

// Focus the password field when page loads
window.focusPasswordField = function() {
    setTimeout(() => {
        const loginField = document.getElementById('loginKey');
        if (loginField) {
            loginField.focus();
        }
    }, 300);
};

// Add subtle animation to the logo
window.animateLogo = function() {
    const logo = document.querySelector('.logo-icon');
    if (logo) {
        logo.style.transition = 'transform 0.5s ease, box-shadow 0.5s ease';
        logo.style.transform = 'scale(1.05)';
        logo.style.boxShadow = '0 5px 15px rgba(0, 102, 204, 0.15)';
        
        setTimeout(() => {
            logo.style.transform = 'scale(1)';
            logo.style.boxShadow = '0 2px 8px rgba(0, 0, 0, 0.05)';
        }, 500);
    }
};

// Apply a simple password strength indicator
window.checkPasswordStrength = function(password) {
    if (!password) return { strength: 'Weak', percentage: 0 };
    
    let score = 0;
    
    // Length check
    score += Math.min(password.length * 2, 20);
    
    // Complexity checks
    if (/[A-Z]/.test(password)) score += 10;
    if (/[a-z]/.test(password)) score += 10;
    if (/[0-9]/.test(password)) score += 10;
    if (/[^A-Za-z0-9]/.test(password)) score += 15;
    
    // Variety bonus
    const uniqueChars = new Set(password).size;
    score += Math.min(uniqueChars * 2, 15);
    
    // Map score to strength
    let strength = 'Weak';
    if (score >= 80) strength = 'Very Strong';
    else if (score >= 60) strength = 'Strong';
    else if (score >= 40) strength = 'Medium';
    else if (score >= 20) strength = 'Weak';
    else strength = 'Very Weak';
    
    return { 
        strength: strength, 
        percentage: Math.min(score, 100) 
    };
};
