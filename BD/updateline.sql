CREATE OR REPLACE FUNCTION public.updateline(i_id integer, v_document_number character varying, v_printed_note character varying)
 RETURNS boolean
 LANGUAGE plpgsql
AS $function$

		declare doctype varchar;
		declare breprint bool := false;

	begin
		
		PERFORM set_config('timezone', 'America/New_York', true);

		select l.document_type, l.reprint into doctype, breprint from PUBLIC."line" l where l.id=i_id;
		
		IF NOT exists (select * from line where document_number=v_document_number and document_type=doctype) 
			or breprint=true or doctype<>'F' then
			UPDATE PUBLIC."line" l
				SET "document_number"=v_document_number,
				"printed_note"=v_printed_note,
				"printed"=true,
				"printed_date"=clock_timestamp()::varchar(19)
			WHERE l."id"=i_id and "printed"=false;
		END IF;

		return true;

	END;
$function$
;
