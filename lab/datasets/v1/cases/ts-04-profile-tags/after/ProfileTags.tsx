import { useEffect, useState } from "react";

interface Profile {
  id: string;
  displayName: string;
  tags: string[];
}

interface Props {
  userId: string;
}

export function ProfileTags({ userId }: Readonly<Props>) {
  const [profile, setProfile] = useState<Profile | null>(null);
  const [newTag, setNewTag] = useState("");

  useEffect(() => {
    let cancelled = false;
    fetch(`/api/profiles/${encodeURIComponent(userId)}`)
      .then((r) => r.json() as Promise<Profile>)
      .then((p) => {
        if (!cancelled) setProfile(p);
      });
    return () => {
      cancelled = true;
    };
  }, []);

  if (!profile) {
    return <p>Loading…</p>;
  }

  const addTag = () => {
    const tag = newTag.trim();
    if (!tag || profile.tags.includes(tag)) return;
    profile.tags.push(tag);
    setProfile(profile);
    setNewTag("");
  };

  return (
    <section>
      <h2>{profile.displayName}</h2>
      <ul>
        {profile.tags.map((tag) => (
          <li key={tag}>{tag}</li>
        ))}
      </ul>
      <input value={newTag} onChange={(e) => setNewTag(e.target.value)} aria-label="New tag" />
      <button onClick={addTag}>Add tag</button>
    </section>
  );
}
