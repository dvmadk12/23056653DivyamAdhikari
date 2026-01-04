// Theme management
window.applyTheme = (theme) => {
    document.documentElement.setAttribute('data-theme', theme);
    document.body.className = theme === 'dark' ? 'dark-theme' : 'light-theme';
};

// Chart.js initialization and helpers
window.drawMoodChart = (canvasId, data) => {
    const canvas = document.getElementById(canvasId);
    if (!canvas) return;

    const ctx = canvas.getContext('2d');
    const centerX = canvas.width / 2;
    const centerY = canvas.height / 2;
    const radius = Math.min(centerX, centerY) - 20;

    // Clear canvas
    ctx.clearRect(0, 0, canvas.width, canvas.height);

    const colors = {
        'Positive': '#22C55E',
        'Neutral': '#94A3B8',
        'Negative': '#EF4444'
    };

    let currentAngle = -Math.PI / 2;
    const total = Object.values(data).reduce((a, b) => a + b, 0);

    if (total === 0) {
        // Draw empty circle
        ctx.beginPath();
        ctx.arc(centerX, centerY, radius, 0, Math.PI * 2);
        ctx.fillStyle = '#E2E8F0';
        ctx.fill();
        return;
    }

    Object.entries(data).forEach(([label, value]) => {
        const sliceAngle = (value / total) * Math.PI * 2;
        
        ctx.beginPath();
        ctx.moveTo(centerX, centerY);
        ctx.arc(centerX, centerY, radius, currentAngle, currentAngle + sliceAngle);
        ctx.closePath();
        ctx.fillStyle = colors[label] || '#E2E8F0';
        ctx.fill();

        // Draw border
        ctx.strokeStyle = '#FFFFFF';
        ctx.lineWidth = 2;
        ctx.stroke();

        currentAngle += sliceAngle;
    });
};

window.drawWordCountChart = (canvasId, data) => {
    const canvas = document.getElementById(canvasId);
    if (!canvas) return;

    const ctx = canvas.getContext('2d');
    const width = canvas.width;
    const height = canvas.height;
    const padding = 40;

    // Clear canvas
    ctx.clearRect(0, 0, width, height);

    const entries = Object.entries(data).sort();
    if (entries.length === 0) return;

    const maxValue = Math.max(...Object.values(data));
    const minValue = Math.min(...Object.values(data));
    const range = maxValue - minValue || 1;

    const chartWidth = width - padding * 2;
    const chartHeight = height - padding * 2;
    const stepX = chartWidth / (entries.length - 1 || 1);

    // Draw grid lines
    ctx.strokeStyle = '#E2E8F0';
    ctx.lineWidth = 1;
    for (let i = 0; i <= 5; i++) {
        const y = padding + (chartHeight / 5) * i;
        ctx.beginPath();
        ctx.moveTo(padding, y);
        ctx.lineTo(width - padding, y);
        ctx.stroke();
    }

    // Draw line
    ctx.beginPath();
    ctx.strokeStyle = '#3B82F6';
    ctx.lineWidth = 2;
    ctx.fillStyle = 'rgba(59, 130, 246, 0.1)';

    entries.forEach(([label, value], index) => {
        const x = padding + stepX * index;
        const normalizedValue = (value - minValue) / range;
        const y = padding + chartHeight - (normalizedValue * chartHeight);

        if (index === 0) {
            ctx.moveTo(x, y);
        } else {
            ctx.lineTo(x, y);
        }
    });

    // Fill area under line
    const lastX = padding + stepX * (entries.length - 1);
    ctx.lineTo(lastX, padding + chartHeight);
    ctx.lineTo(padding, padding + chartHeight);
    ctx.closePath();
    ctx.fill();
    ctx.stroke();

    // Draw points
    ctx.fillStyle = '#3B82F6';
    entries.forEach(([label, value], index) => {
        const x = padding + stepX * index;
        const normalizedValue = (value - minValue) / range;
        const y = padding + chartHeight - (normalizedValue * chartHeight);

        ctx.beginPath();
        ctx.arc(x, y, 4, 0, Math.PI * 2);
        ctx.fill();
    });

    // Draw labels
    ctx.fillStyle = '#718096';
    ctx.font = '12px sans-serif';
    ctx.textAlign = 'center';
    entries.forEach(([label, value], index) => {
        const x = padding + stepX * index;
        const monthLabel = label.split('-')[1];
        ctx.fillText(monthLabel, x, height - padding + 20);
    });
};

