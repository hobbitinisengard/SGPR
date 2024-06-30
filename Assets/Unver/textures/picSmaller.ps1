# Import the necessary module for image manipulation
Add-Type -AssemblyName System.Drawing

# Define the function to resize images
function Resize-Image {
    param (
        [string]$InputFile,
        [string]$OutputFile,
        [int]$NewWidth,
        [int]$NewHeight
    )
    
    # Load the image
    $image = [System.Drawing.Image]::FromFile($InputFile)
    
    # Create a new bitmap with the new dimensions
    $newImage = New-Object System.Drawing.Bitmap($NewWidth, $NewHeight)
    
    # Create a graphics object to draw the new image
    $graphics = [System.Drawing.Graphics]::FromImage($newImage)
    
    # Set the interpolation mode to high quality
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    
    # Draw the resized image
    $graphics.DrawImage($image, 0, 0, $NewWidth, $NewHeight)
    
    # Save the new image to the output file
    $newImage.Save($OutputFile, [System.Drawing.Imaging.ImageFormat]::Png)
    
    # Dispose of the objects to free up resources
    $graphics.Dispose()
    $newImage.Dispose()
    $image.Dispose()
}

# Define the directory to search for images
$directory = 'C:\Users\Kuba\Desktop\SGP-R\STUNT GP REMASTERED\Assets\Unver\textures\16x'

# Get all PNG files in the directory and subdirectories
$pngFiles = Get-ChildItem -Path $directory -Filter *.png

foreach ($file in $pngFiles) {
    # Load the image to check its dimensions
    $image = [System.Drawing.Image]::FromFile($file.FullName)
    
    # Check if the image is 4096x4096
    if ($image.Width -ge 4096 -or $image.Height -ge 4096) {
        # Calculate the new dimensions
        $newWidth = [math]::Round($image.Width / 2)
        $newHeight = [math]::Round($image.Height / 2)
        
        # Define a temporary output file path
        $tempFile = [System.IO.Path]::Combine($file.DirectoryName+'\out\', [System.IO.Path]::GetFileName($file.Name))
        
        # Resize and save the image to the temporary file
        Resize-Image -InputFile $file.FullName -OutputFile $tempFile -NewWidth $newWidth -NewHeight $newHeight
        
		
        # Replace the original file with the resized file
        Write-Output "$tempFile"+"$image.ImageData"
    }
    
    # Dispose of the image object to free up resources
    $image.Dispose()
}