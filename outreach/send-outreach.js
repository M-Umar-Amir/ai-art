/**
 * Epical Studio — B2B Outreach Mailer
 * Usage:  node outreach/send-outreach.js [industry]
 * Industry filters: food | fashion | realestate | tech | all (default: food)
 *
 * Setup:
 *   1. Create a .env file in the project root with:
 *      GMAIL_USER=smart.hire.ai.2025@gmail.com
 *      GMAIL_APP_PASS=xxxx xxxx xxxx xxxx   (Gmail App Password — not your account password)
 *   2. npm install nodemailer dotenv
 *   3. node outreach/send-outreach.js food
 *
 * Get a Gmail App Password at:
 *   Google Account → Security → 2-Step Verification → App passwords
 */

const nodemailer = require('nodemailer');
const fs = require('fs');
const path = require('path');
require('dotenv').config({ path: path.join(__dirname, '../.env') });

const contacts = JSON.parse(fs.readFileSync(path.join(__dirname, 'contacts.json'), 'utf8'));
const industry = process.argv[2] || 'food';

const filtered = industry === 'all'
  ? contacts
  : contacts.filter(c => c.industry === industry);

const templateFile = industry === 'fashion'
  ? path.join(__dirname, 'template-fashion.html')
  : path.join(__dirname, 'template-food.html');

const htmlTemplate = fs.readFileSync(templateFile, 'utf8');

const transporter = nodemailer.createTransport({
  service: 'gmail',
  auth: {
    user: process.env.GMAIL_USER,
    pass: process.env.GMAIL_APP_PASS,
  },
});

const SUBJECT = {
  food: 'Cinematic AI campaign visuals for {{BRAND_NAME}} — Epical Studio, Karachi',
  fashion: 'Campaign imagery & brand films for {{BRAND_NAME}} — Epical Studio',
  realestate: 'Cinematic property visuals for {{BRAND_NAME}} — Epical Studio',
  tech: 'AI-crafted brand visuals for {{BRAND_NAME}} — Epical Studio',
  all: 'Cinematic AI visuals for {{BRAND_NAME}} — Epical Studio, Karachi',
};

const subjectTemplate = SUBJECT[industry] || SUBJECT.all;

async function sendAll() {
  console.log(`\nEpical Studio Outreach — Industry: ${industry} — ${filtered.length} contacts\n`);

  let sent = 0, failed = 0;

  for (const contact of filtered) {
    const html = htmlTemplate.replace(/\{\{BRAND_NAME\}\}/g, contact.brand);
    const subject = subjectTemplate.replace(/\{\{BRAND_NAME\}\}/g, contact.brand);

    try {
      await transporter.sendMail({
        from: `"Epical Studio" <${process.env.GMAIL_USER}>`,
        to: contact.email,
        subject,
        html,
      });
      console.log(`  ✓  ${contact.brand} → ${contact.email}`);
      sent++;
      /* 3-second delay between sends to avoid Gmail rate limits */
      await new Promise(r => setTimeout(r, 3000));
    } catch (err) {
      console.error(`  ✗  ${contact.brand} → ${contact.email}  (${err.message})`);
      failed++;
    }
  }

  console.log(`\nDone — ${sent} sent, ${failed} failed`);
}

sendAll().catch(console.error);
