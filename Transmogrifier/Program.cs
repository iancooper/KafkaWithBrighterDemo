using Transmogrification;

var theBox = new Box();
var theDial = new Dial();

var httpConfig = new HttpConfig
{
    Url = new Uri(" http://localhost:5000")
};

var dispatcher = new Dispatcher(httpConfig);

bool stop = false;

while (!stop)
{

    var settings = new TransmogrificationSettings();
    settings.Name = theBox.AskName();

    settings.Transformation = theDial.AskForTransformation(settings.Name);

    theDial.DisplaySettings(settings);

    theBox.EnterTransmogrifier(settings);

    if (await dispatcher.Transmogrify(settings))
        theBox.MarkTransmogrificationComplete();
    else
        theBox.MarkTransmogrificationFailed();
    
    stop = theBox.AskIfDone();
}