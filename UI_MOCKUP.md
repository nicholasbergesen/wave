# Wave Chat UI - Source Indicators Mockup

## Chat Interface with Source Indicators

```
┌──────────────────────────────────────────────────────────────┐
│  Wave                                                        │
│  [▶ Documents]                                               │
├──────────────────────────────────────────────────────────────┤
│                                                              │
│                                                              │
│                 ┌────────────────────────────────────┐       │
│                 │ What's the latest news about AI?   │       │
│                 └────────────────────────────────────┘       │
│                                                              │
│  ┌──────────────────────────────────────────────────────┐   │
│  │ ┌──────┐                                             │   │
│  │ │🌐 📄 │  ← Source indicators (blue web, orange doc)│   │
│  │ └──────┘                                             │   │
│  │                                                       │   │
│  │ Based on recent web information and your             │   │
│  │ documents, here are the latest AI developments:      │   │
│  │                                                       │   │
│  │ 1. OpenAI announced GPT-5 is in development...       │   │
│  │ 2. Google's Gemini has been updated with...          │   │
│  │ 3. Microsoft invested $10B in AI infrastructure...   │   │
│  └──────────────────────────────────────────────────────┘   │
│                                                              │
│                 ┌────────────────────────────────────┐       │
│                 │ Tell me about Python                │       │
│                 └────────────────────────────────────┘       │
│                                                              │
│  ┌──────────────────────────────────────────────────────┐   │
│  │                                                       │   │
│  │ Python is a high-level programming language          │   │
│  │ created by Guido van Rossum. It's known for...       │   │
│  │                                                       │   │
│  └──────────────────────────────────────────────────────┘   │
│                         (no icons - using training data)     │
│                                                              │
├──────────────────────────────────────────────────────────────┤
│  [Type your message...]                          [Send]     │
└──────────────────────────────────────────────────────────────┘
```

## Icon Details

### Web Search Icon 🌐
- **When shown**: Response used Google search results
- **Color scheme**: Blue (#e3f2fd background, #2196F3 border)
- **Tooltip**: "Used web search"
- **Indicates**: Fresh, real-time information from the web

### RAG Document Icon 📄
- **When shown**: Response used uploaded documents
- **Color scheme**: Orange (#fff3e0 background, #FF9800 border)
- **Tooltip**: "Used documents"
- **Indicates**: Information from user's document library

### Combined Example

When both sources are used:
```
┌──────────────────────────────────────┐
│ ┌──────┐                             │
│ │🌐 📄 │  ← Both icons present       │
│ └──────┘                             │
│                                      │
│ Combining recent web data with       │
│ information from your documents...   │
└──────────────────────────────────────┘
```

### No Sources Example

When only training data is used (no special sources):
```
┌──────────────────────────────────────┐
│                                      │  ← No icons
│ This is general knowledge from       │
│ my training...                       │
└──────────────────────────────────────┘
```

## Responsive Behavior

### Desktop View
- Icons in absolute position (top-left of message bubble)
- Slight offset (-8px) above message
- Horizontal layout with 4px gap between icons

### Mobile View
- Same positioning maintains readability
- Icons scale proportionally
- Touch-friendly hover tooltips

## Icon Specifications

### Size and Spacing
```css
.source-icon {
    font-size: 14px;
    padding: 2px 4px;
    border-radius: 4px;
    border: 1px solid;
    gap: 4px;  /* between multiple icons */
}
```

### Web Search Icon Colors
```css
.source-icon.web-search {
    background: #e3f2fd;  /* Light blue */
    border-color: #2196F3; /* Material blue */
}
```

### RAG Document Icon Colors
```css
.source-icon.rag-doc {
    background: #fff3e0;  /* Light orange */
    border-color: #FF9800; /* Material orange */
}
```

## Interaction States

### Default State
- Icons visible immediately with message
- Subtle box-shadow for depth: `0 1px 2px rgba(0,0,0,0.1)`

### Hover State
- Tooltip appears with description
- No color change (maintains visual stability)

### Focus State (Accessibility)
- Keyboard navigation supported
- Outline for focused icons

## Animation (Future Enhancement)

Potential fade-in animation for icons:
```css
@keyframes fadeIn {
    from { opacity: 0; transform: translateY(-5px); }
    to { opacity: 1; transform: translateY(0); }
}

.source-icon {
    animation: fadeIn 0.3s ease-out;
}
```

## Accessibility

- **ARIA Labels**: Icons include title attributes for screen readers
- **Color Independence**: Distinct emoji characters (🌐, 📄) don't rely solely on color
- **Contrast**: Border colors meet WCAG AA standards
- **Keyboard Navigation**: Icons are focusable for screen reader users

## Browser Compatibility

- **Emoji Support**: 🌐 and 📄 supported in all modern browsers
- **CSS Features**: Flexbox, absolute positioning widely supported
- **Fallback**: If emoji don't render, border colors still distinguish types
