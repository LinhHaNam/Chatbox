import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';
import api from '../services/api';
import ChatSidebar from '../components/ChatSidebar';
import ChatWindow from '../components/ChatWindow';
import '../styles/Chat.css';

const DEFAULT_RUDENESS_STORAGE_KEY = 'defaultRudenessLevel';
const MOBILE_BREAKPOINT = 768;

const getMessageTimestamp = (message) => {
  const value = message?.createdAt;
  const timestamp = value ? new Date(value).getTime() : Number.NaN;
  return Number.isNaN(timestamp) ? 0 : timestamp;
};

const getSessionTimestamp = (session) => {
  const value = session?.lastActiveAt || session?.startedAt;
  const timestamp = value ? new Date(value).getTime() : Number.NaN;
  return Number.isNaN(timestamp) ? 0 : timestamp;
};

const sortMessagesChronologically = (messages = []) =>
  [...messages]
    .map((message, index) => ({ message, index }))
    .sort((left, right) => {
      const timestampDiff = getMessageTimestamp(left.message) - getMessageTimestamp(right.message);
      if (timestampDiff !== 0) {
        return timestampDiff;
      }

      return left.index - right.index;
    })
    .map(({ message }) => message);

const normalizeSession = (session) => ({
  ...session,
  messages: sortMessagesChronologically(session?.messages || []),
});

const sortSessionsByLastActivity = (sessions = []) =>
  [...sessions]
    .map((session, index) => ({ session: normalizeSession(session), index }))
    .sort((left, right) => {
      const timestampDiff = getSessionTimestamp(right.session) - getSessionTimestamp(left.session);
      if (timestampDiff !== 0) {
        return timestampDiff;
      }

      return left.index - right.index;
    })
    .map(({ session }) => session);

export default function ChatPage() {
  const navigate = useNavigate();
  const { isAuthenticated, user, logout } = useAuth();
  const [sessions, setSessions] = useState([]);
  const [currentSessionId, setCurrentSessionId] = useState(null);
  const [currentSession, setCurrentSession] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [isMobileViewport, setIsMobileViewport] = useState(() => window.innerWidth <= MOBILE_BREAKPOINT);
  const [isSidebarCollapsed, setIsSidebarCollapsed] = useState(false);
  const [isSidebarOpenMobile, setIsSidebarOpenMobile] = useState(() => window.innerWidth > MOBILE_BREAKPOINT);

  const getStoredDefaultRudenessLevel = () =>
    localStorage.getItem(DEFAULT_RUDENESS_STORAGE_KEY) || 'Neutral';

  const persistDefaultRudenessLevel = (level) => {
    localStorage.setItem(DEFAULT_RUDENESS_STORAGE_KEY, level);
  };

  const syncSessionState = (updatedSession) => {
    if (!updatedSession?.id) return;

    const normalizedSession = normalizeSession(updatedSession);
    setCurrentSession(normalizedSession);
    setSessions((prevSessions) => {
      const remainingSessions = prevSessions.filter((session) => session.id !== normalizedSession.id);
      return sortSessionsByLastActivity([normalizedSession, ...remainingSessions]);
    });
  };

  useEffect(() => {
    if (!isAuthenticated) {
      navigate('/login');
    }
  }, [isAuthenticated, navigate]);

  useEffect(() => {
    const handleResize = () => {
      const isMobile = window.innerWidth <= MOBILE_BREAKPOINT;
      setIsMobileViewport(isMobile);
      setIsSidebarOpenMobile(!isMobile);
      if (isMobile) {
        setIsSidebarCollapsed(false);
      }
    };

    window.addEventListener('resize', handleResize);
    return () => window.removeEventListener('resize', handleResize);
  }, []);

  useEffect(() => {
    loadSessions();
  }, []);

  useEffect(() => {
    if (currentSessionId) {
      loadSession(currentSessionId);
    } else {
      setCurrentSession(null);
    }
  }, [currentSessionId]);

  useEffect(() => {
    if (sessions.length > 0 && !currentSessionId) {
      setCurrentSessionId(sessions[0].id);
    }
  }, [sessions, currentSessionId]);

  const loadSessions = async () => {
    try {
      setLoading(true);
      const response = await api.getUserSessions();
      if (response.success) {
        setSessions(sortSessionsByLastActivity(response.data || []));
      }
    } catch (err) {
      setError('Khong the tai danh sach cuoc tro chuyen');
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  const loadSession = async (sessionId) => {
    try {
      const response = await api.getChatSession(sessionId);
      if (response?.id) {
        syncSessionState(response);
      }
    } catch (err) {
      setError('Khong the tai cuoc tro chuyen');
      console.error(err);
    }
  };

  const handleCreateNewChat = async () => {
    try {
      const response = await api.createChatSession(getStoredDefaultRudenessLevel());
      if (response?.id) {
        setCurrentSessionId(response.id);
        syncSessionState(response);
        if (isMobileViewport) {
          setIsSidebarOpenMobile(false);
        }
      }
    } catch (err) {
      setError('Khong the tao cuoc tro chuyen moi');
      console.error(err);
    }
  };

  const handleSelectSession = (sessionId) => {
    setCurrentSessionId(sessionId);
    if (isMobileViewport) {
      setIsSidebarOpenMobile(false);
    }
  };

  const handleSendMessage = async (message, rudenessLevel) => {
    if (!currentSessionId) return;

    try {
      const response = await api.sendMessage(currentSessionId, message, rudenessLevel);
      if (response.success) {
        persistDefaultRudenessLevel(rudenessLevel);
        const updatedSession = await api.getChatSession(currentSessionId);
        if (updatedSession?.id) {
          syncSessionState(updatedSession);
        }
      }
    } catch (err) {
      setError('Khong the gui tin nhan');
      console.error(err);
    }
  };

  const handleRudenessLevelChange = async (rudenessLevel) => {
    persistDefaultRudenessLevel(rudenessLevel);

    if (!currentSessionId) {
      return;
    }

    try {
      const updatedSession = await api.updateSessionRudenessLevel(currentSessionId, rudenessLevel);
      if (updatedSession?.id) {
        syncSessionState(updatedSession);
      }
    } catch (err) {
      setError('Khong the cap nhat phong cach AI');
      console.error(err);
    }
  };

  const handleDeleteSession = async (sessionId) => {
    try {
      await api.deleteSession(sessionId);
      const remainingSessions = sortSessionsByLastActivity(
        sessions.filter((session) => session.id !== sessionId)
      );
      setSessions(remainingSessions);

      if (currentSessionId === sessionId) {
        setCurrentSessionId(remainingSessions[0]?.id || null);
      }
    } catch (err) {
      setError('Khong the xoa cuoc tro chuyen');
      console.error(err);
    }
  };

  const handleToggleSidebar = () => {
    if (isMobileViewport) {
      setIsSidebarOpenMobile((prev) => !prev);
      return;
    }

    setIsSidebarCollapsed((prev) => !prev);
  };

  const handleOpenSidebar = () => {
    if (isMobileViewport) {
      setIsSidebarOpenMobile(true);
      return;
    }

    setIsSidebarCollapsed(false);
  };

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  if (!isAuthenticated) {
    return null;
  }

  const showSidebar = isMobileViewport ? isSidebarOpenMobile : !isSidebarCollapsed;

  return (
    <div
      className={`chat-container ${isSidebarCollapsed ? 'sidebar-collapsed' : ''} ${
        isMobileViewport ? 'mobile-layout' : ''
      } ${showSidebar ? 'sidebar-visible' : ''}`}
    >
      {isMobileViewport && isSidebarOpenMobile && (
        <button className="sidebar-backdrop" onClick={handleToggleSidebar} aria-label="Dong sidebar" />
      )}

      <div className={`chat-sidebar-shell ${showSidebar ? 'visible' : ''}`}>
        <ChatSidebar
          sessions={sessions}
          currentSessionId={currentSessionId}
          onSelectSession={handleSelectSession}
          onCreateNew={handleCreateNewChat}
          onDeleteSession={handleDeleteSession}
          user={user}
          onLogout={handleLogout}
          onToggleSidebar={handleToggleSidebar}
          isMobile={isMobileViewport}
        />
      </div>

      <div className="chat-main">
        {error && <div className="error-message">{error}</div>}
        {currentSession ? (
          <ChatWindow
            session={currentSession}
            onSendMessage={handleSendMessage}
            onRudenessLevelChange={handleRudenessLevelChange}
            onOpenSidebar={handleOpenSidebar}
            showSidebarButton={!showSidebar}
            loading={loading}
          />
        ) : (
          <div className="chat-empty">
            {!showSidebar && (
              <button className="chat-empty-toggle" onClick={handleOpenSidebar}>
                Mo danh sach
              </button>
            )}
            <h2>SimsimiChat</h2>
            <p>
              {sessions.length === 0
                ? 'Tao cuoc tro chuyen moi de bat dau'
                : 'Chon mot cuoc tro chuyen hoac tao cuoc tro chuyen moi'}
            </p>
            <button onClick={handleCreateNewChat} className="new-chat-btn">
              + Cuoc tro chuyen moi
            </button>
          </div>
        )}
      </div>
    </div>
  );
}
