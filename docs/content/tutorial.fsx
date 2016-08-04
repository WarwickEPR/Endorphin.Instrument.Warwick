(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#I "../../bin"

(**
# Devices 


## Photon Counter
University of Warwick's Department of Physics Electronics Workshop
have built PIC based counters which will pick up 6ns pulses emitted by
a pair of Excelitas photon counting modules (which have a dead time of
at least 20ns) and tally them. They then produce a voltage proportional
to the count rate and can emit the count rate over USB serial.

This driver can control these devices and stream the count rates emitted
over USB.
*)

use pca = new PhotonCounterAgent("COM3")
pca.Rate() |> Observable.add (printfn "Rate: %d")
pca.InternalTrigger 100 // Set internal clock to trigger every 100ms
pca.Emit() // Start streaming count rates
pca.Silence() // Stop streaming count rates

(**
For more information see the test script
*)
