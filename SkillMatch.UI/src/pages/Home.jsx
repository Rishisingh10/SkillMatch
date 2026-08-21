import { Link } from 'react-router-dom';

const Home = () => {
  return (
    <div style={{ minHeight: 'calc(100vh - 5rem)', position: 'relative', overflow: 'hidden' }}>
      {/* Background vignette */}
      <div className="spotlight-bg" style={{ position: 'absolute', top: 0, left: 0, right: 0, bottom: 0, zIndex: -1 }}></div>

      <div className="container" style={{ position: 'relative', paddingTop: '4rem', paddingBottom: '4rem' }}>
        
        <div style={{ display: 'flex', alignItems: 'flex-start', justifyContent: 'space-between', gap: '2rem' }}>
          
          {/* Huge Typography Area */}
          <div style={{ flex: 1 }}>
            <div style={{ color: 'var(--accent-yellow)', fontSize: '1rem', letterSpacing: '2px', fontWeight: 600, marginBottom: '2rem' }}>
              &#9670; LIVE MATCHING • SINCE 2026
            </div>
            
            <h1 style={{ 
              fontSize: 'clamp(5rem, 10vw, 12rem)', 
              lineHeight: 0.9, 
              color: 'var(--text-light)', 
              marginBottom: 0,
              textTransform: 'none'
            }}>
              Escape<br/>
              the<br/>
              <span style={{ color: 'var(--accent-yellow)' }}>Resume</span><br/>
              <span style={{ color: 'var(--accent-yellow)' }}>Black</span><br/>
              <span style={{ color: 'var(--accent-yellow)' }}>Hole.</span>
            </h1>
          </div>

          {/* Poster graphic placeholder (CSS rotated block to mimic the poster) */}
          <div style={{
            background: 'var(--bg-light)',
            border: '8px solid white',
            boxShadow: '10px 10px 30px rgba(0,0,0,0.5)',
            transform: 'rotate(4deg)',
            width: '300px',
            height: '400px',
            padding: '1rem',
            display: 'flex',
            flexDirection: 'column',
            display: 'none' /* hidden on mobile */
          }} className="retro-poster">
            <h2 style={{ fontSize: '1.2rem', textAlign: 'center', color: 'var(--text-dark)', marginBottom: '1rem' }}>
              SKILLMATCH (SATIRE) • NO. 001 ★★★
            </h2>
            <div style={{ 
              flex: 1, 
              background: 'var(--accent-red)', 
              border: '2px solid var(--text-dark)', 
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              position: 'relative',
              overflow: 'hidden'
            }}>
                <div style={{ 
                    position: 'absolute', 
                    top: '10px', 
                    right: '10px', 
                    background: 'var(--accent-yellow)',
                    color: 'var(--text-dark)',
                    padding: '0.2rem 0.5rem',
                    transform: 'rotate(15deg)',
                    fontFamily: 'var(--font-heading)',
                    border: '2px solid var(--text-dark)'
                }}>
                    APPROVED
                </div>
                <h1 style={{ color: 'var(--bg-light)', fontSize: '5rem', opacity: 0.8 }}>HIRE</h1>
            </div>
          </div>
          
        </div>

      </div>

      {/* Adding a small inline style for media query handling the poster */}
      <style>{`
        @media (min-width: 1024px) {
          .retro-poster {
            display: flex !important;
          }
        }
      `}</style>
    </div>
  );
};

export default Home;
