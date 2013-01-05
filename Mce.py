# Parts of this file contains code from EventGhost, Copyright (C) 2005 Lars-Peter Voss
#   GNU General Public License version 2 or later

from struct import pack
from threading import Thread
import win32event
from MceMessageReceiver import MceMessageReceiver
from pronto import Pronto2MceTimings, ConvertIrCodeToProntoRaw

__author__ = 'Dean Gardiner'


def RoundAndPackTimings(timingData):
    out = ""
    for v in timingData:
        newVal = 50 * int(round(v / 50))
        out += pack("i", newVal)
    return out


class Mce():
    def __init__(self, receive_callback, debug=False):
        self.debug = debug
        self.client = None
        self.receive_callback = receive_callback

        self.ptr_fmt = None
        self.ptr_len = 4

    def close(self):
        pass

    def start(self):
        print "Mce: Starting"
        if self.ptr_fmt is None:  # Need to set this once per installation, depending on 32 or 64 bit OS
            from os import environ

            self.ptr_fmt = "i"  # pack/unpack format for 32 bit int
            if environ.get("PROCESSOR_ARCHITECTURE") == "AMD64" or environ.get("PROCESSOR_ARCHITEW6432") == "AMD64":
                self.ptr_fmt = "q"  # pack/unpack format for 64 bit int
                self.ptr_len = 8

        self.hFinishedEvent = win32event.CreateEvent(None, 1, 0, None)
        self.client = MceMessageReceiver(self.receive_callback, self.hFinishedEvent,
                                         self.ptr_fmt, self.ptr_len,
                                         debug=self.debug)
        self.msgThread = Thread(target=self.client)
        self.msgThread.start()

    def stop(self):
        print "Mce: Stopping"
        win32event.SetEvent(self.hFinishedEvent)
        self.client.Stop()
        self.client = None

    def transmit(self, code="", repeatCount=0):
        #Send pronto code:
        freq, transmitValues = Pronto2MceTimings(code, repeatCount)
        transmitCode = RoundAndPackTimings(transmitValues)
        n = len(transmitCode)
        #Port is set to zero, it is populated automatically
        header = pack(7 * self.ptr_fmt, 2, int(1000000. / freq), 0, 0, 0, 1, len(transmitCode))
        transmitData = header + transmitCode
        self.client.Transmit(transmitData)

    def learn(self, callback):
        if not self.client.ChangeReceiveMode("l".encode("ascii")):
            return False

        # Setup learning
        self.learn_callback = callback
        self.client.learn_callback = self.learn_response

        return True

    def learn_response(self, freqs, code):
        median_freq = sorted(freqs)[len(freqs) / 2]
        print "learn_response", "%d.%03d kHz" % (median_freq / 1000, median_freq % 1000)
        pronto_code = ConvertIrCodeToProntoRaw(median_freq, code)

        self.learn_callback(pronto_code)

        self.client.ChangeReceiveMode("n".encode("ascii"))

        # Reset learning
        self.learn_callback = None
        self.client.learn_callback = None


if __name__ == '__main__':
    def handle(code):
        print "Raw IR Code:", code

    def learn_callback(code):
        print "Pronto Code:", code

    m = Mce(handle, debug=True)
    m.start()

    while True:
        a = raw_input("Command [learn, transmit] > ")
        if a == "learn":
            curLearning = m.learn(learn_callback)
            if curLearning:
                print "learning"
            else:
                print "not able to learn"
        elif a == "transmit":
            code = raw_input("Pronto Code: ")
            m.transmit(code)