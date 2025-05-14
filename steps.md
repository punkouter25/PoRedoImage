Implementation Plan for Coding LLM
[x] Step 1: Project Setup and Configuration
Create a new Blazor Server project targeting .NET 9.0. Set up the solution structure with appropriate namespaces for the image processing application. Configure package references for Azure SDK components including Azure.AI.Vision.ImageAnalysis, Azure.AI.OpenAI, and Microsoft.ApplicationInsights. Create appsettings.json structure for Azure service configurations while ensuring sensitive information is properly secured for deployment.

[x] Step 2: Authentication Configuration
Implement the necessary configuration for Azure App Service Easy Authentication to work with Microsoft and Outlook accounts. Update Program.cs to handle authentication middleware properly. Create a clean login experience that redirects to the main application page upon successful authentication. Consider implementing a simple user profile display showing the authenticated user's name.

[x] Step 3: Main Page Layout Design
Develop the single-page UI layout with responsive design principles for both desktop and mobile viewing. Structure the page with clearly defined sections for image upload, processing status, results display, and error notifications. Implement CSS styles that adapt to different screen sizes, ensuring the portrait mode experience on mobile devices is optimized. Ensure appropriate spacing, typography, and visual hierarchy to create an intuitive user experience.

[x] Step 4: Image Upload Functionality
Implement the file input control with proper validation for file types (PNG and JPEG) and size limit (20MB). Create preview functionality to display the selected image before processing. Develop client-side and server-side validation to ensure only appropriate files are accepted. Implement the description length slider component with default value of 200 words and maximum of 500 words, with real-time display of the selected length.

[x] Step 5: Azure Services Integration - Computer Vision
Integrate the Azure Computer Vision service by implementing the necessary client code to communicate with the API. Create a service class to handle image analysis, extracting captions, tags, and other relevant information from the uploaded image. Implement proper error handling for service failures and unexpected responses. Set up telemetry to track API usage and costs through Application Insights.

[x] Step 6: Azure OpenAI Integration for Description Enhancement
Connect to Azure OpenAI service to refine and enhance the basic description received from Computer Vision. Implement the logic to take the initial image analysis results and generate a more comprehensive and detailed description at the user-specified length. Create appropriate prompts to ensure the description captures the essence of the image in sufficient detail for accurate regeneration. Track token usage for cost monitoring.

[x] Step 7: DALL-E Integration for Image Regeneration
Implement the service client code to communicate with Azure OpenAI's DALL-E model. Create the image generation request using the enhanced description, setting parameters for photorealistic style. Handle the binary image data returned from the service and convert it to a format suitable for display in the browser. Implement proper error handling specific to image generation failures.

[x] Step 8: Progress Tracking and UI Updates
Develop the progress tracking system that updates the UI in real-time during the multi-step processing workflow. Implement progress percentage calculations and stage descriptions. Create the animated progress bar component that visually represents the current state of processing. Ensure the SignalR connection is properly maintained for real-time updates through the Blazor Server framework.

[x] Step 9: Image Download Functionality
Implement JavaScript interop functions to enable downloading of the original image, regenerated image, and side-by-side comparison. Create the logic to generate a side-by-side comparison image by combining the original and regenerated images on a canvas element. Ensure proper file naming and MIME type handling for the downloaded files. Test across different browsers to ensure compatibility.

[x] Step 10: Error Handling, Logging, and Final Polishing
Implement comprehensive error handling throughout the application with user-friendly error messages and detailed technical information for debugging. Set up Application Insights logging for all API calls, capturing usage metrics, response times, and costs. Finalize UI elements with appropriate styling, animations, and feedback mechanisms. Perform thorough testing across devices and browsers to ensure a seamless user experience. Review and optimize Azure resource configurations to minimize costs while maintaining performance.

By following this implementation plan, the coding LLM will be able to develop a complete, functional Blazor Server application that meets all the specified requirements for image translation and regeneration using Azure services, while maintaining a budget-friendly approach suitable for personal use.
