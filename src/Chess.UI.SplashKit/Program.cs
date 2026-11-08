using Chess.UI.SplashKit;

NativeLibrarySetup.Configure(); // must run before anything touches SplashKit

var app = new GameController();
app.Run();
