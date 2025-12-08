# ReplayRenderer
The PC implementation of the Quest's render feature!

# This is a WORK IN PROGRESS
Don't expect it to function! I still need to get some stuff working, make it look pretty and so on (Also, there are A LOT of gaps I need to fill, just like, not implemented at all)

Supports latest modded version, no other versions tested as of right now

# Replay Renderer for Beat Saber

A mod that renders Beat Saber replays to high-quality video frames at any resolution and framerate.

### Quick Start

1. Load a Beat Saber level (any level works for testing)
2. Press **F9** to start rendering
3. Press **F9** again to stop
4. Find your frames in `Documents\Beat Saber\RenderOutput\`

### Controls

- **F9** - Start/Stop rendering
- **F10** - Restart level/replay (when not rendering)
- **F11** - Pause/Resume rendering (during render)
- **H** - Hide/Show game UI (score, combo, energy bar, etc.)
- **Ctrl+P** - Print current camera position (during render)
- **Ctrl+R** - Reset camera to config settings
- **Ctrl+Arrow Keys** - Move camera (during render)
- **Ctrl+PageUp/Down** - Move camera up/down
- **Ctrl +/-** - Adjust FOV

## Future Plans

- [ ] Integration with BeatLeader replay system
- [ ] Integration with ScoreSaber replays
- [ ] Automatic FFmpeg conversion
- [ ] Camera path animation
- [ ] Multiple camera angles
- [ ] Real-time preview
- [ ] GPU-accelerated encoding

## Credits

- Inspired by the Quest Replay mod by [Metalit](https://github.com/Metalit/Replay)
- Built with BSIPA mod framework
- Thanks to the Beat Saber Modding Group

For issues, questions, or suggestions:
- Open an issue on GitHub
