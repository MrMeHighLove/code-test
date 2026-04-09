import { useEffect, useRef, useState } from 'react';
import type { ComponentProps } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { AUTH_PAGE, ROUTES } from '../constants/app';
import { useAuth } from '../context/useAuth';

export default function RegisterPage() {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const { register } = useAuth();
  const navigate = useNavigate();
  const redirectTimeoutRef = useRef<number | null>(null);

  useEffect(() => {
    return () => {
      if (redirectTimeoutRef.current !== null) {
        window.clearTimeout(redirectTimeoutRef.current);
      }
    };
  }, []);

  const handleSubmit: NonNullable<ComponentProps<'form'>['onSubmit']> = async e => {
    e.preventDefault();
    setError('');
    setSuccess('');
    try {
      await register(username, password);
      setSuccess(AUTH_PAGE.registrationSuccess);
      redirectTimeoutRef.current = window.setTimeout(() => navigate(ROUTES.login), 1500);
    } catch (err: unknown) {
      const message = (err as { response?: { data?: { message?: string } } })
        ?.response?.data?.message || AUTH_PAGE.registrationFailed;
      setError(message);
    }
  };

  return (
    <div className="auth-container">
      <h1>{AUTH_PAGE.registerTitle}</h1>
      {error && <p className="error">{error}</p>}
      {success && <p className="success">{success}</p>}
      <form onSubmit={handleSubmit}>
        <input
          type="text"
          placeholder={AUTH_PAGE.registerUsernamePlaceholder}
          value={username}
          onChange={e => setUsername(e.target.value)}
          required
          minLength={3}
        />
        <input
          type="password"
          placeholder={AUTH_PAGE.registerPasswordPlaceholder}
          value={password}
          onChange={e => setPassword(e.target.value)}
          required
          minLength={6}
        />
        <button type="submit">{AUTH_PAGE.registerButton}</button>
      </form>
      <p>{AUTH_PAGE.registerPrompt} <Link to={ROUTES.login}>{AUTH_PAGE.loginButton}</Link></p>
    </div>
  );
}
