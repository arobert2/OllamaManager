### Ollama Model Tray Utility

This utility provides a convenient way to monitor if your Ollama models are currently loaded into VRAM and stop them if needed. It runs in the system tray.

#### How To Use

Add your models to the models.json file.

```json
{
    "models": [
        "ollama model"
    ]
}
```

Run the OllamaModelTrayUtility.exe file. You should see an icon in your system tray. Right-click on the icon to see the list of models you have added to the models.json file. If a model is currently loaded into VRAM, it will be indicated with a checkmark. You can click on a model to stop it from being loaded into VRAM.

Dump it in %programdata%\Microsoft\Windows\Start Menu\Programs\Startup to have it start on boot.
