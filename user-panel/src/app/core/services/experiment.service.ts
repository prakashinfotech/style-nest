import { Injectable } from '@angular/core';

/** ENH-CAT-003 — localStorage keys */
const PARTICIPANT_KEY = 'sn_ab_participant_id';
const VARIANT_PREFIX  = 'sn_ab_';
const EXPIRY_MS       = 30 * 24 * 60 * 60 * 1000; // 30 days

interface StoredVariant { variant: string; expiresAt: number; }

/**
 * ENH-CAT-003 — A/B Variant Framework.
 *
 * Provides stable, deterministic variant assignment for the current browser participant.
 * Assignment algorithm (mirrors backend ExperimentService):
 *   key  = "${experimentName}:${participantId}"
 *   hash = FNV-1a-32(key)
 *   bucket = hash % variants.length
 *
 * Persistence: variant is stored in localStorage with a 30-day TTL so the same
 * participant sees the same variant for the full experiment duration.
 *
 * Usage (synchronous — safe in constructor/field initializer):
 *   private readonly variant = inject(ExperimentService)
 *                               .getVariant('hero-cta', ['A', 'B']);
 */
@Injectable({ providedIn: 'root' })
export class ExperimentService {
  /**
   * Returns the stable variant assignment for the current browser participant.
   * - Reads from localStorage first (experiment duration persistence, 30-day TTL).
   * - Falls back to a deterministic FNV-1a hash of a stable anonymous participant ID.
   * - Synchronous — safe to call from field initializers and constructors.
   */
  getVariant(experimentName: string, variants: readonly string[]): string {
    if (variants.length === 0) return '';
    if (variants.length === 1) return variants[0];

    const stored = this.readStored(experimentName);
    if (stored !== null) return stored;

    const participantId = this.getOrCreateParticipantId();
    const hash   = this.fnv1a32(`${experimentName}:${participantId}`);
    const bucket  = hash % variants.length;
    const variant = variants[bucket];
    this.writeStored(experimentName, variant);
    return variant;
  }

  // ── Private helpers ────────────────────────────────────────────────────────

  private getOrCreateParticipantId(): string {
    try {
      let id = localStorage.getItem(PARTICIPANT_KEY);
      if (!id) {
        id = crypto.randomUUID();
        localStorage.setItem(PARTICIPANT_KEY, id);
      }
      return id;
    } catch {
      // Private browsing / quota exceeded — generate ephemeral ID
      return crypto.randomUUID();
    }
  }

  /** FNV-1a 32-bit — matches server-side ExperimentService.Fnv1a32. */
  private fnv1a32(s: string): number {
    let hash = 2166136261; // FNV offset basis
    for (let i = 0; i < s.length; i++) {
      const lo = s.charCodeAt(i) & 0xff;
      const hi = s.charCodeAt(i) >> 8;
      hash ^= lo;
      hash = Math.imul(hash, 16777619) >>> 0; // FNV prime, keep uint32
      hash ^= hi;
      hash = Math.imul(hash, 16777619) >>> 0;
    }
    return hash; // already unsigned (>>> 0)
  }

  private readStored(name: string): string | null {
    try {
      const raw = localStorage.getItem(`${VARIANT_PREFIX}${name}`);
      if (!raw) return null;
      const val = JSON.parse(raw) as StoredVariant;
      if (Date.now() > val.expiresAt) {
        localStorage.removeItem(`${VARIANT_PREFIX}${name}`);
        return null;
      }
      return val.variant;
    } catch {
      return null;
    }
  }

  private writeStored(name: string, variant: string): void {
    try {
      const val: StoredVariant = { variant, expiresAt: Date.now() + EXPIRY_MS };
      localStorage.setItem(`${VARIANT_PREFIX}${name}`, JSON.stringify(val));
    } catch {
      // Storage quota exceeded or private browsing — non-fatal; variant just won't persist.
    }
  }
}
