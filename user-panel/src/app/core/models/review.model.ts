export interface Review {
  id: string;
  productId: string;
  userId: string;
  author: string;
  rating: number;
  title: string;
  body: string;
  /** ISO date string — mapped from backend `createdAt` */
  date: string;
  /** ENH-PDP-008 — up to 4 photo URLs attached by the reviewer. */
  photoUrls?: string[] | null;
}

export interface CreateReviewRequest {
  rating: number;
  title: string;
  body: string;
  /** ENH-PDP-008 — optional array of up to 4 photo URLs. */
  photoUrls?: string[];
}

export interface PagedReviews {
  items: Review[];
  totalCount: number;
  page: number;
  pageSize: number;
}
