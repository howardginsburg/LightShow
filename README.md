# Party Light Show

## Overview
I created this project to enhance the lighting effects for center pieces at a party.  Essentially a microphone listens to the sounds from music, and sends out commands to a bunch of esp boards that have neopixels connected to them.  In my case, the esp boards were put inside center pieces that had some clear sections.  You can see a detailed walk though of the project at [here](https://youtu.be/BgK8SDP0Mso).

## Architecture
![Architecture Diagram](/Architecture/Lightshow%20Architecture.jpg)

## Building your own
### Equipment
1. Raspberry Pi (2).
2. Touchscreen or screen/keyboard/mouse that are compatible with Raspberry Pi.
3. Webcam or microphone.
4. esp board (1-x)
5. neolights (1-x)

### MQTT Server on Raspberry Pi 1
1. Install Raspbian at https://www.raspberrypi.org/downloads/raspbian/. 
2. Install Mosquitto following the instructions at https://mosquitto.org/blog/2013/01/mosquitto-debian-repository/.
3. Configure the Raspberry Pi as a Wireless Access Point using these [instructions](/wifi.md).

### Music Listener and MQTT Publisher on Raspberry Pi 2
1. Install Windows IoT Core on the Raspberry Pi with the instructions at https://www.microsoft.com/en-us/software-download/windows10iotcore.  Select the LightShow access point for wifi.
2. Connect the webcam/microphone.
3. Install Visual Studio on a development workstation.
4. Open the LightShow.sln and build/compile the code.
5. Change your wifi to the LighShow access point.  Select Raspberry Pi 2 as your deployment target and run.  This will deploy the app onto the Pi.

### ESP deployment
1. Install the Arduino IDE and optionally Visual Studio Code with the Arduino for Visual Studio code Marketplace add-on.
2. Modify your code for the pin you want to connect the neopixel to.
3. Modify your code for the ip address of the wireless access point.
4. Flash the code onto your board.

### Putting it all together
1. Power everything up, play your favorite song, and enjoy the show!
