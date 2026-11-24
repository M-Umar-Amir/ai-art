// Make the shot list seamless: sort by start, each shot ends where the next begins, first starts at 0, last ends at the end.
SHOTS.sort((a, b) => a.t0 - b.t0);
SHOTS.forEach((s, i) => { if (i === 0) s.t0 = 0; s.t1 = i + 1 < SHOTS.length ? SHOTS[i + 1].t0 : B.duration; });
