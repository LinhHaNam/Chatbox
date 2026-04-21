const API_BASE_URL = import.meta.env.VITE_API_URL || '/api';

class ApiClient {
  constructor() {
    this.baseURL = API_BASE_URL;
  }

  getToken() {
    return localStorage.getItem('accessToken');
  }

  setTokens(accessToken, refreshToken) {
    localStorage.setItem('accessToken', accessToken);
    localStorage.setItem('refreshToken', refreshToken);
  }

  clearTokens() {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
  }

  async request(endpoint, options = {}) {
    const headers = {
      'Content-Type': 'application/json',
      ...options.headers,
    };

    const token = this.getToken();
    if (token) {
      headers['Authorization'] = `Bearer ${token}`;
    }

    const response = await fetch(`${this.baseURL}${endpoint}`, {
      ...options,
      headers,
    });

    if (response.status === 401) {
      this.clearTokens();
      window.location.href = '/login';
      throw new Error('Unauthorized');
    }

    const data = await response.json();
    return { status: response.status, data };
  }

  async register(username, password) {
    const { data } = await this.request('/auth/register', {
      method: 'POST',
      body: JSON.stringify({ username, password }),
    });
    return data;
  }

  async login(username, password) {
    const { data } = await this.request('/auth/login', {
      method: 'POST',
      body: JSON.stringify({ username, password }),
    });
    if (data.accessToken && data.refreshToken) {
      this.setTokens(data.accessToken, data.refreshToken);
    }
    return data;
  }

  async refreshToken() {
    const refreshToken = localStorage.getItem('refreshToken');
    if (!refreshToken) throw new Error('No refresh token');

    const { data } = await this.request('/auth/refresh-token', {
      method: 'POST',
      body: JSON.stringify({ refreshToken }),
    });
    if (data.accessToken && data.refreshToken) {
      this.setTokens(data.accessToken, data.refreshToken);
    }
    return data;
  }

  async createChatSession(defaultRudenessLevel = 'Neutral') {
    const { data } = await this.request('/chat/sessions', {
      method: 'POST',
      body: JSON.stringify({ defaultRudenessLevel }),
    });
    return data;
  }

  async getUserSessions() {
    const { data } = await this.request('/chat/sessions');
    return data;
  }

  async getChatSession(sessionId) {
    const { data } = await this.request(`/chat/sessions/${sessionId}`);
    return data;
  }

  async sendMessage(sessionId, content, rudenessLevel = 'Neutral') {
    const { data } = await this.request('/chat/send-message', {
      method: 'POST',
      body: JSON.stringify({
        sessionId,
        content,
        rudenessLevel,
      }),
    });
    return data;
  }

  async updateSessionRudenessLevel(sessionId, rudenessLevel) {
    const { data } = await this.request(`/chat/sessions/${sessionId}/rudeness-level`, {
      method: 'PATCH',
      body: JSON.stringify({ rudenessLevel }),
    });
    return data;
  }

  async deleteSession(sessionId) {
    const { data } = await this.request(`/chat/sessions/${sessionId}`, {
      method: 'DELETE',
    });
    return data;
  }
}

export default new ApiClient();
