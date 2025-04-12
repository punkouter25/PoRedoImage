/**
 * Image processing utility functions
 */

/**
 * Downloads an image from a data URL
 * @param {string} dataUrl - The data URL of the image to download
 * @param {string} fileName - The filename to use for the download
 */
function downloadImage(dataUrl, fileName) {
    const link = document.createElement('a');
    link.href = dataUrl;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
}

/**
 * Creates a side-by-side comparison image and initiates its download
 * @param {string} originalImageUrl - The data URL of the original image
 * @param {string} regeneratedImageUrl - The data URL of the regenerated image
 * @param {string} fileName - The filename to use for the download
 */
function createSideBySideComparison(originalImageUrl, regeneratedImageUrl, fileName) {
    return new Promise((resolve, reject) => {
        const canvas = document.createElement('canvas');
        const ctx = canvas.getContext('2d');
        
        // Create image elements for loading the images
        const originalImage = new Image();
        const regeneratedImage = new Image();
        
        // Count loaded images
        let loadedImages = 0;
        const onImageLoad = () => {
            loadedImages++;
            
            // Once both images are loaded, create the comparison
            if (loadedImages === 2) {
                try {
                    // Set canvas size to fit both images side by side
                    const maxHeight = Math.max(originalImage.height, regeneratedImage.height);
                    const totalWidth = originalImage.width + regeneratedImage.width;
                    
                    canvas.width = totalWidth;
                    canvas.height = maxHeight;
                    
                    // Fill canvas with white background
                    ctx.fillStyle = 'white';
                    ctx.fillRect(0, 0, canvas.width, canvas.height);
                    
                    // Draw original image on the left
                    ctx.drawImage(originalImage, 0, 0);
                    
                    // Draw regenerated image on the right
                    ctx.drawImage(regeneratedImage, originalImage.width, 0);
                    
                    // Add labels
                    ctx.fillStyle = 'black';
                    ctx.font = '16px Arial';
                    ctx.fillText('Original', 10, 20);
                    ctx.fillText('Regenerated', originalImage.width + 10, 20);
                    
                    // Convert to data URL and download
                    const dataUrl = canvas.toDataURL('image/png');
                    downloadImage(dataUrl, fileName);
                    
                    resolve();
                } catch (error) {
                    reject(error);
                }
            }
        };
        
        // Handle load errors
        const onImageError = (error) => {
            reject(error);
        };
        
        // Set up event handlers
        originalImage.onload = onImageLoad;
        regeneratedImage.onload = onImageLoad;
        originalImage.onerror = onImageError;
        regeneratedImage.onerror = onImageError;
        
        // Trigger image loading
        originalImage.src = originalImageUrl;
        regeneratedImage.src = regeneratedImageUrl;
    });
}