// Auto-save functionality for exam answers
class AutoSave {
    constructor() {
        this.saveInterval = 30000; // Save every 30 seconds
        this.maxRetries = 3;
        this.retryDelay = 5000;
        this.isOnline = navigator.onLine;
        this.pendingSaves = new Map();
        this.saveTimer = null;
        
        this.initializeEventListeners();
        this.startAutoSave();
    }

    initializeEventListeners() {
        // Monitor online/offline status
        window.addEventListener('online', () => {
            this.isOnline = true;
            this.processPendingSaves();
        });

        window.addEventListener('offline', () => {
            this.isOnline = false;
        });

        // Save on page visibility change
        document.addEventListener('visibilitychange', () => {
            if (document.visibilityState === 'hidden') {
                this.saveAllAnswers();
            }
        });

        // Save before page unload
        window.addEventListener('beforeunload', (e) => {
            this.saveAllAnswers();
        });

        // Save on answer change
        document.addEventListener('change', (e) => {
            if (e.target.type === 'radio' && e.target.name.startsWith('answers[')) {
                this.saveAnswer(e.target);
            }
        });

        // Save on form submit
        const examForm = document.getElementById('examForm');
        if (examForm) {
            examForm.addEventListener('submit', () => {
                this.saveAllAnswers();
            });
        }
    }

    startAutoSave() {
        if (this.saveTimer) {
            clearInterval(this.saveTimer);
        }

        this.saveTimer = setInterval(() => {
            this.saveAllAnswers();
        }, this.saveInterval);
    }

    stopAutoSave() {
        if (this.saveTimer) {
            clearInterval(this.saveTimer);
            this.saveTimer = null;
        }
    }

    async saveAnswer(radioElement) {
        const questionId = this.extractQuestionId(radioElement.name);
        const optionId = radioElement.value;
        const examId = this.extractExamId();

        if (!questionId || !optionId || !examId) {
            console.error('Could not extract required data for auto-save');
            return;
        }

        const saveData = {
            questionId: questionId,
            optionId: optionId,
            timestamp: new Date().toISOString()
        };

        if (this.isOnline) {
            try {
                await this.sendToServer(saveData);
                this.showSaveIndicator('success');
                this.removePendingSave(questionId);
            } catch (error) {
                console.error('Auto-save failed:', error);
                this.addPendingSave(questionId, saveData);
                this.showSaveIndicator('error');
            }
        } else {
            this.addPendingSave(questionId, saveData);
            this.showSaveIndicator('offline');
        }
    }

    async saveAllAnswers() {
        const answeredQuestions = document.querySelectorAll('input[type="radio"]:checked');
        const savePromises = [];

        answeredQuestions.forEach(radio => {
            savePromises.push(this.saveAnswer(radio));
        });

        try {
            await Promise.all(savePromises);
            console.log('All answers saved successfully');
        } catch (error) {
            console.error('Error saving some answers:', error);
        }
    }

    async sendToServer(saveData) {
        const response = await fetch('/Exam/AutoSaveAnswer', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': this.getAntiForgeryToken()
            },
            body: JSON.stringify(saveData)
        });

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        return response.json();
    }

    addPendingSave(questionId, saveData) {
        this.pendingSaves.set(questionId, saveData);
        this.saveToLocalStorage();
    }

    removePendingSave(questionId) {
        this.pendingSaves.delete(questionId);
        this.saveToLocalStorage();
    }

    async processPendingSaves() {
        if (this.pendingSaves.size === 0) return;

        console.log(`Processing ${this.pendingSaves.size} pending saves`);

        for (const [questionId, saveData] of this.pendingSaves) {
            try {
                await this.sendToServer(saveData);
                this.removePendingSave(questionId);
                this.showSaveIndicator('success');
            } catch (error) {
                console.error(`Failed to save pending answer for question ${questionId}:`, error);
            }
        }
    }

    saveToLocalStorage() {
        const data = {
            pendingSaves: Array.from(this.pendingSaves.entries()),
            timestamp: new Date().toISOString()
        };
        
        try {
            localStorage.setItem('examAutoSave', JSON.stringify(data));
        } catch (error) {
            console.error('Failed to save to localStorage:', error);
        }
    }

    loadFromLocalStorage() {
        try {
            const data = localStorage.getItem('examAutoSave');
            if (data) {
                const parsed = JSON.parse(data);
                this.pendingSaves = new Map(parsed.pendingSaves);
                return true;
            }
        } catch (error) {
            console.error('Failed to load from localStorage:', error);
        }
        return false;
    }

    extractQuestionId(name) {
        const match = name.match(/answers\[(\d+)\]\.SelectedOptionId/);
        return match ? parseInt(match[1]) : null;
    }

    extractExamId() {
        const examIdInput = document.querySelector('input[name="examId"]');
        return examIdInput ? parseInt(examIdInput.value) : null;
    }

    getAntiForgeryToken() {
        const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
        return tokenInput ? tokenInput.value : '';
    }

    showSaveIndicator(status) {
        const indicator = document.getElementById('saveIndicator');
        if (!indicator) {
            const indicatorDiv = document.createElement('div');
            indicatorDiv.id = 'saveIndicator';
            indicatorDiv.style.cssText = `
                position: fixed;
                top: 20px;
                right: 20px;
                padding: 10px 15px;
                border-radius: 5px;
                color: white;
                font-size: 14px;
                z-index: 9999;
                opacity: 0;
                transition: opacity 0.3s;
            `;
            document.body.appendChild(indicatorDiv);
        }

        const indicatorElement = document.getElementById('saveIndicator');
        
        switch (status) {
            case 'success':
                indicatorElement.style.backgroundColor = '#28a745';
                indicatorElement.textContent = 'Answers saved';
                break;
            case 'error':
                indicatorElement.style.backgroundColor = '#dc3545';
                indicatorElement.textContent = 'Save failed - retrying...';
                break;
            case 'offline':
                indicatorElement.style.backgroundColor = '#ffc107';
                indicatorElement.textContent = 'Offline - answers saved locally';
                break;
        }

        indicatorElement.style.opacity = '1';
        
        setTimeout(() => {
            indicatorElement.style.opacity = '0';
        }, 3000);
    }

    cleanup() {
        this.stopAutoSave();
        this.saveAllAnswers();
        localStorage.removeItem('examAutoSave');
    }
}

// Global auto-save instance
let autoSaver = null;

// Initialize auto-save
function initializeAutoSave() {
    if (autoSaver) {
        autoSaver.cleanup();
    }
    
    autoSaver = new AutoSave();
    
    // Load any pending saves from localStorage
    if (autoSaver.loadFromLocalStorage()) {
        console.log('Loaded pending saves from previous session');
        if (navigator.onLine) {
            autoSaver.processPendingSaves();
        }
    }
}

// Manual save function
function autoSave() {
    if (autoSaver) {
        autoSaver.saveAllAnswers();
    }
}

// Cleanup function
function cleanupAutoSave() {
    if (autoSaver) {
        autoSaver.cleanup();
        autoSaver = null;
    }
}

// Initialize on page load
document.addEventListener('DOMContentLoaded', function() {
    if (document.getElementById('examForm')) {
        initializeAutoSave();
    }
});

// Cleanup on page unload
window.addEventListener('unload', cleanupAutoSave);

// Export functions for global use
window.AutoSave = AutoSave;
window.initializeAutoSave = initializeAutoSave;
window.autoSave = autoSave;
window.cleanupAutoSave = cleanupAutoSave;
