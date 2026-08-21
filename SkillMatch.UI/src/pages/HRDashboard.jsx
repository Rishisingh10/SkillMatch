import { useState, useEffect } from 'react';
import { Briefcase, Users, Plus, ChevronRight, Star, TrendingUp, X } from 'lucide-react';

const HRDashboard = () => {
  const [jobs, setJobs] = useState([]);
  const [loading, setLoading] = useState(true);
  
  const [selectedJob, setSelectedJob] = useState(null);
  const [candidates, setCandidates] = useState([]);
  const [loadingCandidates, setLoadingCandidates] = useState(false);
  
  const [showNewJobModal, setShowNewJobModal] = useState(false);
  const [newJob, setNewJob] = useState({ title: '', description: '', minExperienceYears: 0, skills: '' });
  const [creatingJob, setCreatingJob] = useState(false);

  useEffect(() => {
    fetchJobs();
  }, []);

  const fetchJobs = async () => {
    try {
      const res = await fetch('http://localhost:5256/api/Recruiter/1/jobs');
      if (res.ok) {
        const data = await res.json();
        setJobs(data);
      }
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  const handleJobClick = async (job) => {
    setSelectedJob(job);
    setLoadingCandidates(true);
    try {
      const res = await fetch(`http://localhost:5256/api/Recruiter/jobs/${job.id}/ranked-candidates`);
      if (res.ok) {
        const data = await res.json();
        setCandidates(data);
      }
    } catch (err) {
      console.error(err);
    } finally {
      setLoadingCandidates(false);
    }
  };

  const handleCreateJob = async (e) => {
    e.preventDefault();
    setCreatingJob(true);
    
    const skillList = newJob.skills.split(',').map(s => ({
      name: s.trim(),
      isMandatory: true
    })).filter(s => s.name);
    
    const payload = {
      title: newJob.title,
      description: newJob.description,
      minExperienceYears: parseFloat(newJob.minExperienceYears),
      jobType: 'FULL_TIME',
      requiredSkills: skillList
    };

    try {
      const res = await fetch('http://localhost:5256/api/Recruiter/1/jobs', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
      });
      if (res.ok) {
        setShowNewJobModal(false);
        setNewJob({ title: '', description: '', minExperienceYears: 0, skills: '' });
        fetchJobs();
      }
    } catch (err) {
      console.error(err);
    } finally {
      setCreatingJob(false);
    }
  };

  return (
    <div className="container" style={{ padding: '2rem 0', display: 'flex', gap: '2rem', height: 'calc(100vh - 5rem)' }}>
      {/* Left Sidebar: Jobs List */}
      <div style={{ width: '350px', display: 'flex', flexDirection: 'column', gap: '1rem' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <h2>HR Portal</h2>
          <button className="retro-button solid" style={{ padding: '0.5rem' }} onClick={() => setShowNewJobModal(true)}>
            <Plus size={20} />
          </button>
        </div>
        
        <div className="solid-border hard-shadow" style={{ flex: 1, overflowY: 'auto', padding: '1rem', display: 'flex', flexDirection: 'column', gap: '1rem', backgroundColor: '#fff' }}>
          {loading && <p>Loading jobs...</p>}
          {!loading && jobs.length === 0 && <p>No jobs posted yet.</p>}
          
          {jobs.map(job => (
            <div 
              key={job.id} 
              className={`solid-border ${selectedJob?.id === job.id ? 'hard-shadow' : ''}`}
              style={{ 
                padding: '1rem', 
                cursor: 'pointer',
                backgroundColor: selectedJob?.id === job.id ? 'var(--bg-light)' : '#fff',
                borderColor: selectedJob?.id === job.id ? 'var(--primary)' : 'var(--border-color)',
                transition: 'all 0.2s ease'
              }}
              onClick={() => handleJobClick(job)}
            >
              <h3 style={{ margin: '0 0 0.5rem 0', fontSize: '1.1rem' }}>{job.title}</h3>
              <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '0.85rem', color: 'var(--text-muted)' }}>
                <span style={{ display: 'flex', alignItems: 'center', gap: '0.25rem' }}><Users size={14} /> {job.applicationCount} Applicants</span>
                <span>Exp: {job.minExperienceYears} yrs</span>
              </div>
            </div>
          ))}
        </div>
      </div>

      {/* Right Content: Ranked Candidates */}
      <div style={{ flex: 1, display: 'flex', flexDirection: 'column' }}>
        {!selectedJob ? (
          <div className="solid-border hard-shadow" style={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', backgroundColor: '#fff' }}>
            <div style={{ textAlign: 'center', color: 'var(--text-muted)' }}>
              <TrendingUp size={48} style={{ margin: '0 auto 1rem' }} />
              <h3>Select a job to view ranked candidates</h3>
            </div>
          </div>
        ) : (
          <div className="solid-border hard-shadow" style={{ flex: 1, overflowY: 'auto', padding: '2rem', backgroundColor: '#fff' }}>
            <h2 style={{ marginTop: 0 }}>{selectedJob.title} - Candidates</h2>
            <p style={{ color: 'var(--text-muted)', marginBottom: '2rem' }}>Candidates are ranked automatically by our AI semantic engine.</p>
            
            {loadingCandidates && <p>Ranking candidates...</p>}
            
            {!loadingCandidates && candidates.length === 0 && (
              <p>No one has applied to this job yet.</p>
            )}
            
            <div style={{ display: 'flex', flexDirection: 'column', gap: '1.5rem' }}>
              {candidates.map((candidate, index) => (
                <div key={candidate.applicationId} className="solid-border" style={{ padding: '1.5rem', display: 'flex', gap: '1.5rem', alignItems: 'flex-start', backgroundColor: index === 0 ? 'var(--bg-light)' : '#fff' }}>
                  <div style={{ 
                    backgroundColor: index === 0 ? 'var(--accent-red)' : 'var(--primary)', 
                    color: '#fff', 
                    width: '40px', height: '40px', 
                    display: 'flex', alignItems: 'center', justifyContent: 'center', 
                    fontSize: '1.25rem', fontWeight: 'bold', border: '2px solid var(--border-color)',
                    boxShadow: '2px 2px 0 var(--border-color)'
                  }}>
                    #{index + 1}
                  </div>
                  
                  <div style={{ flex: 1 }}>
                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '0.5rem' }}>
                      <h3 style={{ margin: 0 }}>{candidate.name}</h3>
                      <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', background: 'var(--primary)', color: '#fff', padding: '0.25rem 0.75rem', fontWeight: 'bold', border: '2px solid var(--border-color)' }}>
                        <Star size={16} fill="#fff" /> {candidate.overallMatchScore}% MATCH
                      </div>
                    </div>
                    
                    <div style={{ display: 'flex', gap: '1rem', fontSize: '0.85rem', marginBottom: '1rem', flexWrap: 'wrap' }}>
                      <span style={{ padding: '0.2rem 0.5rem', background: '#e5e7eb', border: '1px solid #d1d5db' }}>Skill Score: {candidate.skillScore}%</span>
                      <span style={{ padding: '0.2rem 0.5rem', background: '#e5e7eb', border: '1px solid #d1d5db' }}>Experience: {candidate.experienceScore}%</span>
                      <span style={{ padding: '0.2rem 0.5rem', background: '#e5e7eb', border: '1px solid #d1d5db' }}>Semantic Fit: {candidate.semanticFitScore}%</span>
                    </div>

                    <div style={{ background: '#f9fafb', padding: '1rem', border: '1px solid #e5e7eb', fontSize: '0.9rem', lineHeight: '1.5' }}>
                      <strong>AI Insight:</strong> {candidate.explanation}
                    </div>
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}
      </div>

      {/* New Job Modal */}
      {showNewJobModal && (
        <div style={{
          position: 'fixed', top: 0, left: 0, right: 0, bottom: 0, 
          backgroundColor: 'rgba(0,0,0,0.5)', zIndex: 100, 
          display: 'flex', alignItems: 'center', justifyContent: 'center'
        }}>
          <div className="solid-border hard-shadow" style={{ backgroundColor: 'var(--bg-light)', padding: '2rem', width: '90%', maxWidth: '600px', position: 'relative' }}>
            <button onClick={() => setShowNewJobModal(false)} style={{ position: 'absolute', top: '1rem', right: '1rem', background: 'none', border: 'none', cursor: 'pointer' }}>
              <X size={24} />
            </button>
            
            <h3 style={{ marginTop: 0 }}>Post a New Job</h3>
            <form onSubmit={handleCreateJob} style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
              <div>
                <label style={{ display: 'block', fontWeight: 'bold', marginBottom: '0.5rem' }}>Job Title</label>
                <input required type="text" className="solid-border" style={{ width: '100%', padding: '0.75rem', fontFamily: 'inherit' }} value={newJob.title} onChange={e => setNewJob({...newJob, title: e.target.value})} />
              </div>
              
              <div>
                <label style={{ display: 'block', fontWeight: 'bold', marginBottom: '0.5rem' }}>Description</label>
                <textarea required className="solid-border" style={{ width: '100%', padding: '0.75rem', fontFamily: 'inherit', minHeight: '100px' }} value={newJob.description} onChange={e => setNewJob({...newJob, description: e.target.value})}></textarea>
              </div>

              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem' }}>
                <div>
                  <label style={{ display: 'block', fontWeight: 'bold', marginBottom: '0.5rem' }}>Min. Experience (Years)</label>
                  <input required type="number" min="0" step="0.5" className="solid-border" style={{ width: '100%', padding: '0.75rem', fontFamily: 'inherit' }} value={newJob.minExperienceYears} onChange={e => setNewJob({...newJob, minExperienceYears: e.target.value})} />
                </div>
                <div>
                  <label style={{ display: 'block', fontWeight: 'bold', marginBottom: '0.5rem' }}>Required Skills (Comma separated)</label>
                  <input required type="text" placeholder="e.g. React, C#, SQL" className="solid-border" style={{ width: '100%', padding: '0.75rem', fontFamily: 'inherit' }} value={newJob.skills} onChange={e => setNewJob({...newJob, skills: e.target.value})} />
                </div>
              </div>

              <button type="submit" className="retro-button solid" style={{ marginTop: '1rem', justifyContent: 'center' }} disabled={creatingJob}>
                {creatingJob ? 'POSTING...' : 'POST JOB'}
              </button>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};

export default HRDashboard;
