import api from './client';

export const login = (username: string, password: string) =>
  api.post('/api/auth/login', { username, password });

export const register = (username: string, password: string) =>
  api.post('/api/auth/register', { username, password });

export const logout = () => api.post('/api/auth/logout');
