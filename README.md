# OCR control for Windows 10 Universal Apps for marking with ink
A control for Windows 10 universal apps to mark sections on an image by e.g. pen and then run optical character recognition (OCR) on the marked section. 

# Who needs this?
OCR is amazingly good in the meantime. I could imagine several apps which could leverage this. Think about your travel expense reports. About banking stuff. 
I built this control to show how easy it is to get started by implementing a control which tackles all challenges and allows you to either start from the nuget package or from a bunch of source code but not from scratch. 
Please: USE THIS CODE as inspiration for your own project. It's free! :-) As always I'd be happy to hear from you if you're using this or if you used parts of it in an app or product you produced.    


#Known Issues
1. This is currently a demo, a prerelease, a beta. Consider it as inspiration, don't exspect production quality.
2. Drag & Drop works from folders that can be accessed by the app only. If your app has Pictures Library capabilities, drag a picture from there. 
3. The debugging canvas shows up at the wrong position sometimes, due to resizing/format errors. 
4. During cropping an exception might occur due to an error in the normalization algorithm.
5. Currently a file is created for cropping and not cleaned up afterwards.
1. Source code should be cleaned up.
1. Zoom, resize, move, rotation of images is currently possible via touch only.
1. The OcrImageContainer control can not be limited in size. 
1. OCR works great sometimes and not at all on other images.
1. When using online OCR detection, you need a subscription key for computer vision for http://www.projectoxford.ai . And you need internet connection.
1. When using offline mode results may vary in quality. 

# This is how you work with it:
1. Download the nuget package (here: https://www.nuget.org/packages/Dmx.Win.MPC.InkToOcr/0.0.1) or get the source code.
1. Reference the library Dmx.Win.MPC.InkToOcr from your Windows 10 universal app.
1. Add this to your MainPage.xaml where all the other namespaces are referenced
 * > xmlns:dmxocr="using:Dmx.Win.MPC.InkToOcr"
1. Add this where you want to add the control:
 * >   *&lt;dmxocr:OcrImageContainer Name="MyOIC" Width="300" Height="300" UseOnlineOcr="True" SubscriptionKey="*&lt;your project oxford subscription key>">*&lt;/dmxocr:OcrImageContainer>
1. Integrate a button which calls MyOIC.ExtractFromLastStroke(); somewhere
2. Make sure your app has Pictures Library capabilities. 
1. Run the app.
1. Drag an image from your library to the control (it's read. You will notice it.)
1. You can resize it, move it, zoom, rotate it with your fingers until you find a text of interest.
1. Mark the text by drawing a circle around it.
1. Click the button you created. You get the recognized text in return.
1. Questions? Ask.      
