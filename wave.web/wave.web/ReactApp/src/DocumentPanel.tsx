import { useState, useEffect } from 'react'
import './DocumentPanel.css'

type Document = {
    id: string
    fileName: string
    uploadedAt: string
    fileSize: number
}

type DocumentPanelProps = {
    isOpen: boolean
    onToggle: () => void
}

function DocumentPanel({ isOpen, onToggle }: DocumentPanelProps) {
    const [documents, setDocuments] = useState<Document[]>([])
    const [uploading, setUploading] = useState(false)

    useEffect(() => {
        if (isOpen) {
            loadDocuments()
        }
    }, [isOpen])

    const loadDocuments = async () => {
        try {
            const response = await fetch('https://localhost:7141/api/documents')
            const data = await response.json()
            setDocuments(data)
        } catch (err) {
            console.error('Error loading documents:', err)
        }
    }

    const handleFileUpload = async (event: React.ChangeEvent<HTMLInputElement>) => {
        const file = event.target.files?.[0]
        if (!file) return

        setUploading(true)
        const formData = new FormData()
        formData.append('file', file)

        try {
            const response = await fetch('https://localhost:7141/api/documents/upload', {
                method: 'POST',
                body: formData,
            })
            if (response.ok) {
                await loadDocuments()
            }
        } catch (err) {
            console.error('Error uploading document:', err)
        } finally {
            setUploading(false)
        }
    }

    const handleDelete = async (id: string) => {
        try {
            const response = await fetch(`https://localhost:7141/api/documents/${id}`, {
                method: 'DELETE',
            })
            if (response.ok) {
                await loadDocuments()
            }
        } catch (err) {
            console.error('Error deleting document:', err)
        }
    }

    const formatFileSize = (bytes: number) => {
        if (bytes < 1024) return bytes + ' B'
        if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(2) + ' KB'
        return (bytes / (1024 * 1024)).toFixed(2) + ' MB'
    }

    return (
        <>
            <button className="panel-toggle" onClick={onToggle}>
                {isOpen ? '◀' : '▶'} Documents
            </button>
            <div className={`document-panel ${isOpen ? 'open' : ''}`}>
                <div className="panel-header">
                    <h2>Documents</h2>
                </div>
                <div className="panel-content">
                    <div className="upload-section">
                        <input
                            type="file"
                            id="file-upload"
                            onChange={handleFileUpload}
                            accept=".txt,.md"
                            style={{ display: 'none' }}
                        />
                        <label htmlFor="file-upload" className="upload-button">
                            {uploading ? 'Uploading...' : 'Upload Document'}
                        </label>
                    </div>
                    <div className="documents-list">
                        {documents.map((doc) => (
                            <div key={doc.id} className="document-item">
                                <div className="document-info">
                                    <div className="document-name">{doc.fileName}</div>
                                    <div className="document-meta">
                                        {formatFileSize(doc.fileSize)} • {new Date(doc.uploadedAt).toLocaleDateString()}
                                    </div>
                                </div>
                                <button
                                    className="delete-button"
                                    onClick={() => handleDelete(doc.id)}
                                >
                                    ×
                                </button>
                            </div>
                        ))}
                        {documents.length === 0 && !uploading && (
                            <div className="empty-state">No documents uploaded yet</div>
                        )}
                    </div>
                </div>
            </div>
        </>
    )
}

export default DocumentPanel
