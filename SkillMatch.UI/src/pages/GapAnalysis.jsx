import { useState, useEffect } from 'react';
import { useParams } from 'react-router-dom';
import { Target, CheckCircle2, XCircle, AlertCircle } from 'lucide-react';

const ScoreRing = ({ score, label, color }) => {
  const radius = 40;
  const circumference = 2 * Math.PI * radius;
  const strokeDashoffset = circumference - (score / 100) * circumference;

  return (
    <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '1rem' }}>
      <div style={{ position: 'relative', width: '120px', height: '120px' }}>
        <svg style={{ transform: 'rotate(-90deg)', width: '100%', height: '100%' }}>
          <circle 
            cx="60" cy="60" r={radius} 
            stroke="rgba(255,255,255,0.1)" 
            strokeWidth="8" fill="none" 
          />
          <circle 
            cx="60" cy="60" r={radius} 
            stroke={color} 
            strokeWidth="8" fill="none" 
            strokeDasharray={circumference}
            strokeDashoffset={strokeDashoffset}
            style={{ transition: 'stroke-dashoffset 1s ease-out' }}
          />
        </svg>
        <div style={{ position: 'absolute', top: 0, left: 0, width: '100%', height: '100%', display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: '1.5rem', fontWeight: 'bold' }}>
          {score}%
        </div>
      </div>
      <span style={{ fontWeight: 500, color: 'var(--text-muted)' }}>{label}</span>
    </div>
  );
};

const GapAnalysis = () => {
  const { jobId } = useParams();
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    const fetchAnalysis = async () => {
      try {
        // Hardcoded candidate 1 and job 1 for demonstration if not provided
        const jId = jobId || 1;
        const response = await fetch(`http://localhost:5256/api/Candidate/1/gap-analysis/${jId}`);
        if (!response.ok) {
          throw new Error('Analysis could not be retrieved. Ensure you seeded the DB with a Candidate and Job.');
        }
        const result = await response.json();
        setData(result);
      } catch (err) {
        setError(err.message);
      } finally {
        setLoading(false);
      }
    };
    
    fetchAnalysis();
  }, [jobId]);

  if (loading) return <div className="container flex-center" style={{ height: '50vh' }}>Loading analysis with AI Engine...</div>;
  
  if (error) return (
    <div className="container" style={{ padding: '2rem 0' }}>
      <div className="solid-border hard-shadow" style={{ textAlign: 'center', color: 'var(--accent-red)', padding: '2rem' }}>
        <AlertCircle size={48} style={{ margin: '0 auto 1rem auto' }} />
        <h3>Failed to load analysis</h3>
        <p>{error}</p>
        <p style={{ fontSize: '0.9rem', color: 'var(--text-dark)' }}>Did you create Job ID {jobId || 1} and Candidate ID 1 in the API first?</p>
      </div>
    </div>
  );

  return (
    <div className="container" style={{ padding: '2rem 0', paddingBottom: '6rem' }}>
      <div className="animate-fade-up">
        <div style={{ display: 'flex', alignItems: 'center', gap: '1rem', marginBottom: '2rem' }}>
          <Target size={32} color="var(--accent-red)" />
          <div>
            <h2 style={{ marginBottom: 0 }}>Match Analysis: {data.jobTitle}</h2>
            <p style={{ margin: 0 }}>Candidate vs Job Requirement Breakdown</p>
          </div>
        </div>

        {/* Scores Overview */}
        <div className="solid-border hard-shadow" style={{ marginBottom: '2rem', padding: '2rem' }}>
          <h3 style={{ marginBottom: '2rem', textAlign: 'center' }}>Match Scores</h3>
          <div style={{ display: 'flex', justifyContent: 'space-around', flexWrap: 'wrap', gap: '2rem' }}>
            <ScoreRing score={data.matchSummary.overallScore} label="Overall Match" color="var(--accent-yellow)" />
            <ScoreRing score={data.matchSummary.skillMatchScore} label="Skill Overlap" color="var(--accent-red)" />
            <ScoreRing score={data.matchSummary.semanticFitScore} label="AI Semantic Fit" color="var(--text-dark)" />
            <ScoreRing score={data.matchSummary.experienceFitScore} label="Experience Fit" color="var(--text-dark)" />
          </div>
        </div>

        {/* Explanation */}
        <div className="solid-border hard-shadow animate-fade-up delay-1" style={{ marginBottom: '2rem', background: 'var(--accent-red)', color: 'var(--text-light)', padding: '2rem' }}>
          <h3 style={{ marginBottom: '1rem', color: 'var(--accent-yellow)' }}>AI Insight</h3>
          <p style={{ margin: 0, fontSize: '1.1rem', color: 'var(--text-light)', fontStyle: 'italic', textTransform: 'none' }}>
            "{data.explanation}"
          </p>
        </div>

        {/* Skills Breakdown */}
        <div className="grid-2 animate-fade-up delay-2">
          <div className="solid-border hard-shadow" style={{ padding: '2rem' }}>
            <h3 style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
              <CheckCircle2 color="var(--accent-red)" /> Matched Skills
            </h3>
            <ul style={{ listStyle: 'none', marginTop: '1.5rem', display: 'flex', flexDirection: 'column', gap: '1rem' }}>
              {data.skillBreakdown.matchedSkills.length > 0 ? data.skillBreakdown.matchedSkills.map((skill, idx) => (
                <li key={idx} style={{ padding: '0.75rem', background: 'var(--bg-dark)', color: 'var(--text-light)', border: '2px solid var(--text-dark)', fontWeight: 500 }}>
                  {skill}
                </li>
              )) : <li style={{ color: 'var(--text-dark)' }}>No skills matched.</li>}
            </ul>
          </div>

          <div className="solid-border hard-shadow" style={{ padding: '2rem' }}>
            <h3 style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
              <XCircle color="var(--text-dark)" /> Missing Skills
            </h3>
            <ul style={{ listStyle: 'none', marginTop: '1.5rem', display: 'flex', flexDirection: 'column', gap: '1rem' }}>
              {data.skillBreakdown.missingSkills.length > 0 ? data.skillBreakdown.missingSkills.map((skill, idx) => (
                <li key={idx} style={{ padding: '0.75rem', background: 'var(--accent-yellow)', color: 'var(--text-dark)', border: '2px solid var(--text-dark)', fontWeight: 500 }}>
                  {skill} {data.skillBreakdown.missingMandatorySkills.includes(skill) && <span style={{ fontSize: '0.75rem', marginLeft: '0.5rem', padding: '0.1rem 0.4rem', background: 'var(--accent-red)', color: 'var(--text-light)' }}>MANDATORY</span>}
                </li>
              )) : <li style={{ color: 'var(--accent-red)' }}>Candidate has all required skills!</li>}
            </ul>
          </div>
        </div>

      </div>
    </div>
  );
};

export default GapAnalysis;
