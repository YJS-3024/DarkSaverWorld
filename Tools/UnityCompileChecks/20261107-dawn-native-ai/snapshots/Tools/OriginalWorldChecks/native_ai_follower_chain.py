"""Original full follower path, including real24-byte movement builder."""
import struct
from native_ai_frame_chain import NativeAiQueueUser
from native_ai_emulator import UC_HOOK_CODE
class NativeAiFollowerQueue(NativeAiQueueUser):
    def _dispatch(self,uc,address,size,data):
        if self.hooks.get(address)=='send-move':
            pointer=self.args(1)[0];values=struct.unpack('<11h',uc.mem_read(pointer,22))
            self.input.append(dict(kind='move',source=values[0],points=[list(values[i:i+2])for i in range(1,11,2)]))
            return # Execute actual04FF0 builder and final send boundary.
        super()._dispatch(uc,address,size,data)
