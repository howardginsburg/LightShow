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
2. Install Mosquitto following the instructions at https://mosquitto.org/blog/2013/01/mosquitto-debian-repository/
3. Configure the Raspberry Pi 
