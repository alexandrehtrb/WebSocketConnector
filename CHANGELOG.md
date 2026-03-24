# Changelog

* [1.2.0](#120-2026-03-24)
* [1.1.1](#111-2026-03-21)
* [1.1.0](#110-2026-03-20)
* [1.0.2](#102-2026-03-06)
* [1.0.1](#101-2026-03-04)
* [1.0.0](#100-2026-03-03)

## 1.2.0 (2026-03-24)

- Performance: Remove unnecessary periodic WebSocket state check.
- Performance: Remove unnecessary try-catches.
- Fix: Use `ws.CloseOutputAsync()` to prevent eternal waiting for remote close frame.
- Fix: Collect sent closure message.
- Tests: Add `[DebuggerDisplay]` attribute on `WebSocketMessage`.
- Tests: Allow run TestClient from both command line and Visual Studio.
- Docs: Add Keep-Alive explanations.

## 1.1.1 (2026-03-21)

- Feature: Customizable buffer size.
- Performance: Remove initial capacity from accumulator MemoryStream when receiving messages.

## 1.1.0 (2026-03-20)

- Feature: `msg.FormatForLogging()` method.
- Performance: `WebSocketMessage` backed by byte arrays when possible, to avoid instantiating MemoryStreams.
- Performance: Accumulator MemoryStream with initial size when receiving messages.
- Tests: TestClient tests split in many classes and with parallel runs.
- Tests: Prevent file access collisions in FullConversationTest.
- Tests: TestServer single-line console log messages.

## 1.0.2 (2026-03-06)

- Feature: Collect messages only from the opposite side.

## 1.0.1 (2026-03-04)

- Performance: Reuse `MemoryStream accumulator` in the constructor of received messages.
- Tests: Add `ServerExceptionConversation` test.

## 1.0.0 (2026-03-03)

First release!
