# ControllerEmulator #Daydream #iOS

<img src="https://lh3.googleusercontent.com/Wk2fg-bOLQR4gROWP91gfXLKoAlFLsvSzgMoMOtgrNY_5zWIl-H1yxIJM6BcBO_kB7drbaPYhCivio5UmqGWFM4OEc0QuA=s688" alt="Daydream logo" width="200"/>

This application is a lite copy of the one given by @GoogleVR who only works on #Android.

/!\ There is now another branch called "optimized" : less memory allocation => less garbage collection calls ; all unused assets removed => compilation time particularly reduced /!\

Because I haven't 2 Android phones, but a Nexus 4 and an iPhone 4 (who can't be the hmd because it's not powerful enough), this project let me use my old iOS as a remote controller for #Daydream apps like the Labs Controller Playground (given on github @GoogleVR for Unity).

After some tests, this seems to works well. Maybe the latency can be improved (if it's not my phone) with a better management of thread. There isn't visual feedback for touch because when you have an HMD, you can't see the screen of the controller... The click on touchpad can have some troubles (I've see some problems when I've tested the pottery game of Labs Controller Playground, but don't really know if it's related to latency or bugs).

This took me 2 days of work (some reverse engineering on network messaging system, understand more deeply the event system (and event trigger) of unity). Each builds on my very old mac taking 45 minutes to 1 hour (that's why 2 days was necessary...).

I can now focus on making some #Daydream apps!

This work is under the MIT Licence. Do what you want with it!
