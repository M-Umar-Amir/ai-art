# Dallas Dash — static export

Deploy steps, fastest first.

## 1. As its own Vercel project

    npm i -g vercel
    vercel deploy --prod          # run inside this folder

Or drag the folder onto https://vercel.com/new. No framework preset, no build
command, no environment variables: it is plain static files.

## 2. Inside your existing Vercel site (subfolder)

Copy this folder's contents to:

* `<your-project>/public/dallas-dash/`   (Vite, Next, CRA — anything served from public/)
* `<your-project>/dallas-dash/`          (a plain static project)

Then it is live at `https://yourdomain.com/dallas-dash/`. The paths in the export
are relative, so no rewriting is needed.

## 3. Link it

From your site's nav or a button: `<a href="/dallas-dash/">Play Dallas Dash</a>`,
or embed it with an iframe sized 9:16.

## What this build is, and what it is not

* It is the real game: the same simulation, the same verified-replay reward rule,
  the same coupon flow.
* The account and its points live in the visitor's browser (localStorage), per
  device. Clearing site data starts a fresh account, and a determined visitor
  can edit it — so a coupon from this build proves nothing to a restaurant.
* The hosted build is the one that can: there the ledger sits on the server, every
  submitted run is replayed there, and a fabricated score is refused. Use this
  export to demo the experience; use the hosted build for anything involving real
  discounts.
* No brand assets are used: "Big D Fried" is a fictional restaurant drawn in code.
  Keep the on-page "concept build" note.
