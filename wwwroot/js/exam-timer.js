// Exam Timer JavaScript
class ExamTimer {
    constructor(duration, startTime) {
        this.duration = duration * 60; // Convert minutes to seconds
        this.startTime = new Date(startTime);
        this.endTime = new Date(this.startTime.getTime() + this.duration * 1000);
        this.remainingTime = 0;
        this.timerInterval = null;
        this.isExpired = false;
    }

    start() {
        this.updateTimer();
        this.timerInterval = setInterval(() => this.updateTimer(), 1000);
    }

    updateTimer() {
        const now = new Date();
        this.remainingTime = Math.max(0, Math.floor((this.endTime - now) / 1000));

        if (this.remainingTime <= 0) {
            this.isExpired = true;
            this.stop();
            this.onTimeExpired();
        }

        this.updateDisplay();
    }

    updateDisplay() {
        const minutes = Math.floor(this.remainingTime / 60);
        const seconds = this.remainingTime % 60;
        const display = `${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`;
        
        const timeDisplay = document.getElementById('timeDisplay');
        if (timeDisplay) {
            timeDisplay.textContent = display;
            
            // Add warning classes
            if (this.remainingTime <= 300) { // 5 minutes
                timeDisplay.parentElement.classList.add('text-danger');
                timeDisplay.parentElement.classList.remove('text-warning', 'text-success');
            } else if (this.remainingTime <= 600) { // 10 minutes
                timeDisplay.parentElement.classList.add('text-warning');
                timeDisplay.parentElement.classList.remove('text-danger', 'text-success');
            } else {
                timeDisplay.parentElement.classList.add('text-success');
                timeDisplay.parentElement.classList.remove('text-warning', 'text-danger');
            }
        }

        // Update progress bar if exists
        const progressBar = document.getElementById('timerProgress');
        if (progressBar) {
            const totalDuration = this.duration;
            const elapsed = totalDuration - this.remainingTime;
            const percentage = (elapsed / totalDuration) * 100;
            progressBar.style.width = percentage + '%';
        }
    }

    stop() {
        if (this.timerInterval) {
            clearInterval(this.timerInterval);
            this.timerInterval = null;
        }
    }

    onTimeExpired() {
        // Auto-submit exam when time expires
        const examForm = document.getElementById('examForm');
        if (examForm) {
            // Show warning before auto-submitting
            if (confirm('Time has expired! Your exam will be automatically submitted.')) {
                examForm.submit();
            } else {
                examForm.submit(); // Submit anyway after confirmation
            }
        }
    }

    getRemainingMinutes() {
        return Math.floor(this.remainingTime / 60);
    }

    getRemainingSeconds() {
        return this.remainingTime % 60;
    }
}

// Global timer instance
let examTimer = null;

// Initialize timer function
function initializeTimer(duration, startTime) {
    if (examTimer) {
        examTimer.stop();
    }
    
    examTimer = new ExamTimer(duration, startTime);
    examTimer.start();
    
    // Store timer data for page refresh scenarios
    localStorage.setItem('examTimer', JSON.stringify({
        duration: duration,
        startTime: startTime,
        pageLoadTime: new Date().toISOString()
    }));
}

// Restore timer on page load
function restoreTimer() {
    const timerData = localStorage.getItem('examTimer');
    if (timerData) {
        const data = JSON.parse(timerData);
        const elapsed = (new Date() - new Date(data.pageLoadTime)) / 1000;
        const remainingDuration = data.duration - elapsed;
        
        if (remainingDuration > 0) {
            initializeTimer(remainingDuration, data.startTime);
        }
    }
}

// Clean up timer data
function cleanupTimer() {
    localStorage.removeItem('examTimer');
    if (examTimer) {
        examTimer.stop();
        examTimer = null;
    }
}

// Auto-save timer state every 30 seconds
setInterval(() => {
    if (examTimer && !examTimer.isExpired) {
        localStorage.setItem('examTimerState', JSON.stringify({
            remainingTime: examTimer.remainingTime,
            timestamp: new Date().toISOString()
        }));
    }
}, 30000);

// Handle page visibility changes
document.addEventListener('visibilitychange', function() {
    if (document.hidden) {
        // Page is hidden, pause any non-critical operations
        console.log('Page hidden - timer continues in background');
    } else {
        // Page is visible, check timer status
        if (examTimer && examTimer.isExpired) {
            alert('Your exam time has expired! The exam will be submitted automatically.');
        }
    }
});

// Handle browser close/refresh
window.addEventListener('beforeunload', function(e) {
    if (examTimer && !examTimer.isExpired) {
        e.preventDefault();
        e.returnValue = 'Your exam is still in progress. Are you sure you want to leave?';
        return e.returnValue;
    }
});

// Keyboard shortcuts
document.addEventListener('keydown', function(e) {
    // Ctrl+S to save current progress
    if (e.ctrlKey && e.key === 's') {
        e.preventDefault();
        if (typeof autoSave === 'function') {
            autoSave();
        }
    }
    
    // Prevent Alt+Tab and other navigation shortcuts during exam
    if (e.altKey && (e.key === 'Tab' || e.key === 'F4')) {
        e.preventDefault();
        alert('Navigation shortcuts are disabled during the exam.');
    }
});

// Disable right-click during exam
document.addEventListener('contextmenu', function(e) {
    if (examTimer && !examTimer.isExpired) {
        e.preventDefault();
        return false;
    }
});

// Fullscreen detection
function detectFullscreen() {
    if (!document.fullscreenElement && examTimer && !examTimer.isExpired) {
        alert('Please keep the exam in fullscreen mode for security purposes.');
    }
}

// Request fullscreen on exam start
function requestFullscreen() {
    const elem = document.documentElement;
    if (elem.requestFullscreen) {
        elem.requestFullscreen().catch(err => {
            console.log('Fullscreen request failed:', err);
        });
    }
}

// Monitor fullscreen changes
document.addEventListener('fullscreenchange', detectFullscreen);

// Export functions for global use
window.ExamTimer = ExamTimer;
window.initializeTimer = initializeTimer;
window.restoreTimer = restoreTimer;
window.cleanupTimer = cleanupTimer;
window.requestFullscreen = requestFullscreen;
