# Changelog

* [1.1.0](#110-2026-03-20)
* [1.0.2](#102-2026-03-06)
* [1.0.1](#101-2026-03-04)
* [1.0.0](#100-2026-03-03)

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
