# UI Enhancements - Source Indicators

## Overview
The chat interface now displays visual indicators when web search or RAG documents are used to generate responses.

## Visual Design

### Source Indicator Icons

**Web Search Icon** 🌐
- Appears when the response was enhanced with web search results
- Blue background (#e3f2fd) with blue border (#2196F3)
- Hover text: "Used web search"

**RAG Document Icon** 📄
- Appears when the response used RAG documents
- Orange background (#fff3e0) with orange border (#FF9800)
- Hover text: "Used documents"

### Layout

Icons appear in the **upper left corner** of assistant message bubbles:

```
┌─────────────────────────────────────┐
│ 🌐 📄                                │  ← Source indicators (top-left)
│                                      │
│ [Assistant's response content here]  │
│ Lorem ipsum dolor sit amet...        │
│                                      │
└─────────────────────────────────────┘
```

### Multiple Sources Example

When both web search and RAG documents are used:

```
┌─────────────────────────────────────┐
│ 🌐 📄                                │  ← Both icons displayed
│                                      │
│ Based on recent web information     │
│ and your documents, here's what...  │
│                                      │
└─────────────────────────────────────┘
```

### No Sources Example

When only the model's training data is used (no indicators):

```
┌─────────────────────────────────────┐
│                                      │  ← No icons
│ This is a standard response based   │
│ on my training data...               │
│                                      │
└─────────────────────────────────────┘
```

## Technical Implementation

### Frontend Changes (React/TypeScript)

**Message Type Updated:**
```typescript
type Message = {
    role: 'user' | 'assistant'
    content: string
    usedWebSearch?: boolean  // NEW
    usedRag?: boolean         // NEW
}
```

**Component Rendering:**
```tsx
<div className="msg-header">
    {msg.role === 'assistant' && (msg.usedWebSearch || msg.usedRag) && (
        <div className="source-indicators">
            {msg.usedWebSearch && (
                <span className="source-icon web-search" title="Used web search">
                    🌐
                </span>
            )}
            {msg.usedRag && (
                <span className="source-icon rag-doc" title="Used documents">
                    📄
                </span>
            )}
        </div>
    )}
</div>
```

### Backend Changes (C#)

**Message Model Updated:**
```csharp
public class Message
{
    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("usedWebSearch")]
    public bool UsedWebSearch { get; set; }  // NEW

    [JsonPropertyName("usedRag")]
    public bool UsedRag { get; set; }         // NEW
}
```

**ChatController Tracking:**
```csharp
// Track sources used
bool usedWebSearch = false;
bool usedRag = false;

// ... perform searches ...

// Return message with metadata
var message = new Message()
{
    Role = vllmMessage.Role,
    Content = vllmMessage.Content,
    UsedWebSearch = usedWebSearch,
    UsedRag = usedRag
};
```

## CSS Styling

```css
.msg {
    position: relative;  /* For absolute positioning of icons */
}

.msg-header {
    position: absolute;
    top: -8px;          /* Positions icons above the message bubble */
    left: 8px;
}

.source-indicators {
    display: flex;
    gap: 4px;           /* Space between multiple icons */
}

.source-icon {
    display: inline-block;
    font-size: 14px;
    background: white;
    border: 1px solid #ccc;
    border-radius: 4px;
    padding: 2px 4px;
    line-height: 1;
    box-shadow: 0 1px 2px rgba(0,0,0,0.1);
}

.source-icon.web-search {
    background: #e3f2fd;
    border-color: #2196F3;
}

.source-icon.rag-doc {
    background: #fff3e0;
    border-color: #FF9800;
}
```

## User Benefits

1. **Transparency**: Users immediately see which sources contributed to each response
2. **Trust**: Knowing when web search was used builds confidence in real-time information
3. **Context**: Understanding when documents were used helps users track information sources
4. **Non-intrusive**: Icons don't interfere with message content readability

## Future Enhancements

Potential improvements for future iterations:

1. **Clickable Icons**: Show detailed source information on click
2. **Source List**: Display specific URLs or documents used
3. **Color Coding**: Different colors for different types of sources
4. **Animation**: Subtle animation when icons appear
5. **Badge Counts**: Show number of sources used (e.g., "3 🌐")
