import React from 'react';
import '../styles/ChatSidebar.css';
import { formatVietnamRelativeDate } from '../utils/dateTime';

export default function ChatSidebar({
  sessions,
  currentSessionId,
  onSelectSession,
  onCreateNew,
  onDeleteSession,
  user,
  onLogout,
  onToggleSidebar,
  isMobile,
}) {
  const getSessionDate = (session) => session?.lastActiveAt || session?.startedAt;

  const getSortedMessages = (messages = []) =>
    [...messages]
      .map((message, index) => ({ message, index }))
      .sort((left, right) => {
        const leftTime = left.message?.createdAt ? new Date(left.message.createdAt).getTime() : 0;
        const rightTime = right.message?.createdAt ? new Date(right.message.createdAt).getTime() : 0;

        if (leftTime !== rightTime) {
          return leftTime - rightTime;
        }

        return left.index - right.index;
      })
      .map(({ message }) => message);

  const getSessionTitle = (session) => {
    const firstUserMessage = getSortedMessages(session?.messages || []).find(
      (message) => message.senderType === 'User' && message.content?.trim()
    );

    if (!firstUserMessage) {
      return 'Cuoc tro chuyen moi';
    }

    return firstUserMessage.content.length > 24
      ? `${firstUserMessage.content.slice(0, 24)}...`
      : firstUserMessage.content;
  };

  return (
    <div className="chat-sidebar">
      <div className="sidebar-header">
        <h1>SimsimiChat</h1>
        <div className="sidebar-actions">
          <button onClick={onCreateNew} className="new-chat-btn-small" title="Cuoc tro chuyen moi">
            +
          </button>
          <button
            onClick={onToggleSidebar}
            className="sidebar-toggle-btn"
            title={isMobile ? 'Dong danh sach' : 'Thu gon danh sach'}
          >
            {isMobile ? '×' : '←'}
          </button>
        </div>
      </div>

      <div className="sessions-list">
        {sessions.length === 0 ? (
          <p className="no-sessions">Chua co cuoc tro chuyen nao</p>
        ) : (
          sessions.map((session) => (
            <div
              key={session.id}
              className={`session-item ${currentSessionId === session.id ? 'active' : ''}`}
              onClick={() => onSelectSession(session.id)}
            >
              <div className="session-info">
                <div className="session-title">{getSessionTitle(session)}</div>
                <div className="session-date">{formatVietnamRelativeDate(getSessionDate(session))}</div>
              </div>
              <button
                className="delete-btn"
                onClick={(e) => {
                  e.stopPropagation();
                  if (confirm('Xoa cuoc tro chuyen nay?')) {
                    onDeleteSession(session.id);
                  }
                }}
              >
                ×
              </button>
            </div>
          ))
        )}
      </div>

      <div className="sidebar-footer">
        {user && (
          <div className="user-info">
            <span>{user.username}</span>
          </div>
        )}
        <button onClick={onLogout} className="logout-btn">
          Dang xuat
        </button>
      </div>
    </div>
  );
}
